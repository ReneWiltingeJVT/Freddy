using FluentAssertions;
using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Application.Features.Chat.Commands;
using Freddy.Application.Features.Chat.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Freddy.Application.Tests.Features.Chat.Commands;

public sealed class SendMessageCommandHandlerTests
{
    private readonly IConversationRepository _conversationRepository = Substitute.For<IConversationRepository>();
    private readonly IPackageRepository _packageRepository = Substitute.For<IPackageRepository>();
    private readonly IDocumentRepository _documentRepository = Substitute.For<IDocumentRepository>();
    private readonly IPackageRouter _packageRouter = Substitute.For<IPackageRouter>();
    private readonly ISmallTalkDetector _smallTalkDetector = Substitute.For<ISmallTalkDetector>();
    private readonly IClientDetector _clientDetector = Substitute.For<IClientDetector>();
    private readonly IPackageResponseFormatter _packageResponseFormatter = Substitute.For<IPackageResponseFormatter>();
    private readonly IKnowledgeContextBuilder _knowledgeContextBuilder = Substitute.For<IKnowledgeContextBuilder>();
    private readonly IChatResponseGenerator _chatResponseGenerator = Substitute.For<IChatResponseGenerator>();
    private readonly SendMessageCommandHandler _handler;

    public SendMessageCommandHandlerTests()
    {
        _smallTalkDetector.Detect(Arg.Any<string>()).Returns(SmallTalkResult.NoMatch);
        _clientDetector.DetectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ClientDetectionResult.NoMatch);

        // Default: batch document name loading returns empty dictionary
        _documentRepository
            .GetNamesByPackageIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, List<string>>());

        // Default: formatter produces structured Dutch response from package data
        _packageResponseFormatter
            .Format(Arg.Any<Package>())
            .Returns(callInfo =>
            {
                Package p = callInfo.Arg<Package>();
                return $"Ik heb het volgende pakket gevonden:\n\n**{p.Title}**\n\n{p.Description}\n\n{p.Content}".TrimEnd();
            });

        // Default: knowledge context returns minimal context
        _knowledgeContextBuilder
            .BuildAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new KnowledgeContext("Geen pakketten", "Geen cliënten", string.Empty));

        // Default: chat history returns empty
        _conversationRepository
            .GetRecentMessagesAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Default: LLM generates a response only for no-match / overview queries (matched packages use the formatter)
        _chatResponseGenerator
            .GenerateAsync(Arg.Any<ChatResponseRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponseResult(
                "Sorry, ik kon geen passend antwoord vinden op je vraag.",
                null,
                false));

        _handler = new SendMessageCommandHandler(
            _conversationRepository,
            _packageRepository,
            _documentRepository,
            _packageRouter,
            _smallTalkDetector,
            _clientDetector,
            _packageResponseFormatter,
            _knowledgeContextBuilder,
            _chatResponseGenerator,
            NullLogger<SendMessageCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_HighConfidenceMatch_ReturnsPackageContent()
    {
        // Arrange
        var conversationId = Guid.CreateVersion7();
        var packageId = Guid.CreateVersion7();
        var conversation = new Conversation
        {
            Id = conversationId,
            UserId = Guid.CreateVersion7(),
            Title = "Test",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var package = new Package
        {
            Id = packageId,
            Title = "Voedselbank",
            Description = "Protocol voor voedselbankpakketten",
            Content = "Stap 1: Check voorraad.",
            IsPublished = true,
        };

        _conversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _conversationRepository.AddMessageAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Message>());
        _packageRepository.GetAllPublishedAsync(Arg.Any<CancellationToken>())
            .Returns([package]);
        _packageRepository.GetByIdAsync(packageId, Arg.Any<CancellationToken>())
            .Returns(package);
        _packageRouter.RouteAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<PackageCandidate>>(), Arg.Any<CancellationToken>())
            .Returns(new PackageRouterResult
            {
                ChosenPackageId = packageId,
                Confidence = 0.9,
                NeedsConfirmation = false,
                Reason = "Duidelijke match",
            });
        _documentRepository.GetByPackageIdAsync(packageId, Arg.Any<CancellationToken>())
            .Returns([]);

        SendMessageCommand command = new(conversationId, "Hoe werkt de voedselbank?");

        // Act
        Result<MessageDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be("assistant");
        result.Value.Content.Should().Contain("Voedselbank");
        result.Value.Content.Should().Contain("Check voorraad");
    }

    [Fact]
    public async Task Handle_HighConfidenceMatch_IncludesDocumentOffer()
    {
        // Arrange
        var conversationId = Guid.CreateVersion7();
        var packageId = Guid.CreateVersion7();
        var conversation = new Conversation
        {
            Id = conversationId,
            UserId = Guid.CreateVersion7(),
            Title = "Test",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var package = new Package
        {
            Id = packageId,
            Title = "Voedselbank",
            Description = "Protocol voor voedselbankpakketten",
            Content = "Stap 1: Check voorraad.",
            IsPublished = true,
        };

        _conversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _conversationRepository.AddMessageAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Message>());
        _packageRepository.GetAllPublishedAsync(Arg.Any<CancellationToken>())
            .Returns([package]);
        _packageRepository.GetByIdAsync(packageId, Arg.Any<CancellationToken>())
            .Returns(package);
        _packageRouter.RouteAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<PackageCandidate>>(), Arg.Any<CancellationToken>())
            .Returns(new PackageRouterResult
            {
                ChosenPackageId = packageId,
                Confidence = 0.9,
                NeedsConfirmation = false,
                Reason = "Duidelijke match",
            });
        _documentRepository.GetByPackageIdAsync(packageId, Arg.Any<CancellationToken>())
            .Returns(
            [
                new Document
                {
                    Id = Guid.CreateVersion7(),
                    PackageId = packageId,
                    Name = "Informatiepakket",
                    FileUrl = "/uploads/documents/info.pdf",
                    Type = DocumentType.Pdf,
                    CreatedAt = DateTimeOffset.UtcNow,
                },
            ]);

        SendMessageCommand command = new(conversationId, "Hoe werkt de voedselbank?");

        // Act
        Result<MessageDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().Contain("document beschikbaar");
        result.Value.Content.Should().Contain("Informatiepakket");
    }

    [Fact]
    public async Task Handle_ServiceUnavailable_LlmFallback()
    {
        // Arrange
        // When the router returns IsServiceUnavailable (defensive path — CompositePackageRouter
        // normally handles this before it reaches the handler), the handler must still return
        // a user-friendly fallback rather than an error or exception.
        var conversationId = Guid.CreateVersion7();
        var conversation = new Conversation
        {
            Id = conversationId,
            UserId = Guid.CreateVersion7(),
            Title = "Test",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _conversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _conversationRepository.AddMessageAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Message>());
        _packageRepository.GetAllPublishedAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        _packageRouter.RouteAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<PackageCandidate>>(), Arg.Any<CancellationToken>())
            .Returns(new PackageRouterResult
            {
                ChosenPackageId = null,
                Confidence = 0.0,
                IsServiceUnavailable = true,
                NeedsConfirmation = false,
                Reason = "AI-service is niet bereikbaar.",
            });

        // LLM should still generate a response from knowledge context (no matched package)
        _chatResponseGenerator
            .GenerateAsync(Arg.Any<ChatResponseRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponseResult("Ik kon helaas geen specifiek antwoord vinden.", null, false));

        SendMessageCommand command = new(conversationId, "Hoe werkt de voedselbank?");

        // Act
        Result<MessageDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert — must succeed (not throw), and return a user-friendly message
        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNotFound()
    {
        // Arrange
        var conversationId = Guid.CreateVersion7();
        _conversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);

        SendMessageCommand command = new(conversationId, "Test message");

        // Act
        Result<MessageDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task Handle_NoPackageMatch_LlmGeneratesResponse()
    {
        // Arrange
        var conversationId = Guid.CreateVersion7();
        var conversation = new Conversation
        {
            Id = conversationId,
            UserId = Guid.CreateVersion7(),
            Title = "Test",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _conversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _conversationRepository.AddMessageAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Message>());
        _packageRepository.GetAllPublishedAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        _packageRouter.RouteAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<PackageCandidate>>(), Arg.Any<CancellationToken>())
            .Returns(new PackageRouterResult
            {
                ChosenPackageId = null,
                Confidence = 0.0,
                NeedsConfirmation = false,
                Reason = "Geen pakketten beschikbaar.",
            });

        SendMessageCommand command = new(conversationId, "Wat is het weer?");

        // Act
        Result<MessageDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().Contain("Sorry");
        await _chatResponseGenerator.Received(1)
            .GenerateAsync(Arg.Any<ChatResponseRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MediumConfidence_ReturnsConfirmationMessage()
    {
        // Arrange
        var conversationId = Guid.CreateVersion7();
        var packageId = Guid.CreateVersion7();
        var conversation = new Conversation
        {
            Id = conversationId,
            UserId = Guid.CreateVersion7(),
            Title = "Test",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var package = new Package
        {
            Id = packageId,
            Title = "Voedselbank",
            Description = "Protocol voor voedselbankpakketten",
            Content = "Stap 1: Check voorraad.",
            IsPublished = true,
            RequiresConfirmation = true, // Package requires confirmation
        };

        _conversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _conversationRepository.AddMessageAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Message>());
        _packageRepository.GetAllPublishedAsync(Arg.Any<CancellationToken>())
            .Returns([package]);
        _packageRepository.GetByIdAsync(packageId, Arg.Any<CancellationToken>())
            .Returns(package);
        _packageRouter.RouteAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<PackageCandidate>>(), Arg.Any<CancellationToken>())
            .Returns(new PackageRouterResult
            {
                ChosenPackageId = packageId,
                Confidence = 0.7,
                NeedsConfirmation = true,
                Reason = "Mogelijk match",
            });

        SendMessageCommand command = new(conversationId, "Iets over eten");

        // Act
        Result<MessageDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().Contain("Klopt dat?");
        result.Value.Content.Should().Contain("Voedselbank");
    }

    [Fact]
    public async Task Handle_RouterReturnsInvalidPackageId_FallsBackToLlm()
    {
        // Arrange — router returns a PackageId that does not exist in the database.
        // The handler should gracefully fall back to the LLM with no matched package context.
        var conversationId = Guid.CreateVersion7();
        var fakePackageId = Guid.CreateVersion7();
        var conversation = new Conversation
        {
            Id = conversationId,
            UserId = Guid.CreateVersion7(),
            Title = "Test",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _conversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _conversationRepository.AddMessageAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Message>());
        _packageRepository.GetAllPublishedAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        _packageRepository.GetByIdAsync(fakePackageId, Arg.Any<CancellationToken>())
            .Returns((Package?)null);
        _packageRouter.RouteAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<PackageCandidate>>(), Arg.Any<CancellationToken>())
            .Returns(new PackageRouterResult
            {
                ChosenPackageId = fakePackageId,
                Confidence = 0.9,
                NeedsConfirmation = false,
                Reason = "Fake match",
            });

        SendMessageCommand command = new(conversationId, "Test");

        // Act
        Result<MessageDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert — LLM generates a response without matched package, includes "Sorry"
        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().NotBeNullOrEmpty();
        await _chatResponseGenerator.Received(1)
            .GenerateAsync(
                Arg.Is<ChatResponseRequest>(r => r.MatchedPackageTitle == null),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SmallTalkMessage_ReturnsTemplateWithoutCallingRouter()
    {
        // Arrange
        var conversationId = Guid.CreateVersion7();
        var conversation = new Conversation
        {
            Id = conversationId,
            UserId = Guid.CreateVersion7(),
            Title = "Test",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _conversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _conversationRepository.AddMessageAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Message>());

        var smallTalkResult = new SmallTalkResult(SmallTalkCategory.Greeting, "Hoi! \uD83D\uDC4B Waarmee kan ik je helpen?");
        _smallTalkDetector.Detect("hallo").Returns(smallTalkResult);

        SendMessageCommand command = new(conversationId, "hallo");

        // Act
        Result<MessageDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be("assistant");
        result.Value.Content.Should().Be("Hoi! \uD83D\uDC4B Waarmee kan ik je helpen?");
        await _packageRouter.DidNotReceive().RouteAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<PackageCandidate>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MediumConfidence_WithRequiresConfirmationFalse_DeliversDirectly()
    {
        // Arrange
        var conversationId = Guid.CreateVersion7();
        var packageId = Guid.CreateVersion7();
        var conversation = new Conversation
        {
            Id = conversationId,
            UserId = Guid.CreateVersion7(),
            Title = "Test",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var package = new Package
        {
            Id = packageId,
            Title = "Voedselbank",
            Description = "Protocol voor voedselbankpakketten",
            Content = "Stap 1: Check voorraad.",
            IsPublished = true,
            RequiresConfirmation = false, // Key: confirmation disabled
        };

        _conversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _conversationRepository.AddMessageAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Message>());
        _packageRepository.GetAllPublishedAsync(Arg.Any<CancellationToken>())
            .Returns([package]);
        _packageRepository.GetByIdAsync(packageId, Arg.Any<CancellationToken>())
            .Returns(package);
        _documentRepository.GetByPackageIdAsync(packageId, Arg.Any<CancellationToken>())
            .Returns([]);
        _packageRouter.RouteAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<PackageCandidate>>(), Arg.Any<CancellationToken>())
            .Returns(new PackageRouterResult
            {
                ChosenPackageId = packageId,
                Confidence = 0.7, // Medium confidence
                NeedsConfirmation = true, // Router suggests confirmation
                Reason = "Mogelijk match",
            });
        _smallTalkDetector.Detect(Arg.Any<string>()).Returns(SmallTalkResult.NoMatch);

        SendMessageCommand command = new(conversationId, "Iets over eten");

        // Act
        Result<MessageDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Should deliver directly without asking "Klopt dat?"
        result.Value!.Content.Should().NotContain("Klopt dat?");
        result.Value.Content.Should().Contain("Voedselbank");
        result.Value.Content.Should().Contain("Check voorraad");
    }
}
