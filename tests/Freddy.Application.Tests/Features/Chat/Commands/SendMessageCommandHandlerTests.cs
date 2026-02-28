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
    private readonly IPackageRouter _packageRouter = Substitute.For<IPackageRouter>();
    private readonly SendMessageCommandHandler _handler;

    public SendMessageCommandHandlerTests()
    {
        _handler = new SendMessageCommandHandler(
            _conversationRepository,
            _packageRepository,
            _packageRouter,
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
            Name = "Voedselbank",
            Description = "Protocol voor voedselbankpakketten",
            Content = "Stap 1: Check voorraad.",
            IsActive = true,
        };

        _conversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _conversationRepository.AddMessageAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Message>());
        _packageRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
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

        SendMessageCommand command = new(conversationId, "Hoe werkt de voedselbank?");

        // Act
        Result<MessageDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be("assistant");
        result.Value.Content.Should().Contain("Voedselbank");
        result.Value.Content.Should().Contain("Stap 1: Check voorraad.");
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
    public async Task Handle_NoPackageMatch_ReturnsFallbackMessage()
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
        _packageRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
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
            Name = "Voedselbank",
            Description = "Protocol voor voedselbankpakketten",
            Content = "Stap 1: Check voorraad.",
            IsActive = true,
        };

        _conversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _conversationRepository.AddMessageAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Message>());
        _packageRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
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
    public async Task Handle_RouterReturnsInvalidPackageId_ReturnsFallback()
    {
        // Arrange
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
        _packageRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
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

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().Contain("fout");
    }
}
