using FluentAssertions;
using FluentValidation.Results;
using Freddy.Application.Features.Chat.Commands;
using Xunit;

namespace Freddy.Application.Tests.Features.Chat.Commands;

public sealed class SendMessageCommandValidatorTests
{
    private readonly SendMessageCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        // Arrange
        SendMessageCommand command = new(Guid.CreateVersion7(), "Hoe werkt het protocol?");

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyContent_IsInvalid()
    {
        // Arrange
        SendMessageCommand command = new(Guid.CreateVersion7(), string.Empty);

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Content");
    }

    [Fact]
    public void Validate_ContentTooLong_IsInvalid()
    {
        // Arrange
        string longContent = new('A', 2001);
        SendMessageCommand command = new(Guid.CreateVersion7(), longContent);

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Content");
    }

    [Fact]
    public void Validate_EmptyConversationId_IsInvalid()
    {
        // Arrange
        SendMessageCommand command = new(Guid.Empty, "Hello");

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConversationId");
    }
}
