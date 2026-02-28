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
    private readonly IConversationRepository _repository = Substitute.For<IConversationRepository>();
    private readonly IChatService _chatService = Substitute.For<IChatService>();
    private readonly SendMessageCommandHandler _handler;

    public SendMessageCommandHandlerTests()
    {
        _handler = new SendMessageCommandHandler(
            _repository,
            _chatService,
            NullLogger<SendMessageCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ValidMessage_ReturnsAssistantMessage()
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

        _repository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _repository.AddMessageAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Message>());
        _chatService.GetResponseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("AI antwoord"));

        SendMessageCommand command = new(conversationId, "Hoe werkt het protocol?");

        // Act
        Result<MessageDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be("assistant");
        result.Value.Content.Should().Be("AI antwoord");
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNotFound()
    {
        // Arrange
        var conversationId = Guid.CreateVersion7();
        _repository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);

        SendMessageCommand command = new(conversationId, "Test message");

        // Act
        Result<MessageDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task Handle_AiServiceFails_ReturnsFallbackMessage()
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

        _repository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _repository.AddMessageAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Message>());
        _chatService.GetResponseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure("LLM error"));

        SendMessageCommand command = new(conversationId, "Test message");

        // Act
        Result<MessageDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().Contain("Sorry");
    }
}
