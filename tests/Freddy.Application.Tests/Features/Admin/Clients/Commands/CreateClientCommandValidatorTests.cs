using FluentAssertions;
using FluentValidation.Results;
using Freddy.Application.Features.Admin.Clients.Commands;
using Xunit;

namespace Freddy.Application.Tests.Features.Admin.Clients.Commands;

public sealed class CreateClientCommandValidatorTests
{
    private readonly CreateClientCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        // Arrange
        var command = new CreateClientCommand(
            DisplayName: "Jan de Vries",
            Aliases: ["jan", "devries"]);

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyDisplayName_IsInvalid()
    {
        // Arrange
        var command = new CreateClientCommand(
            DisplayName: string.Empty,
            Aliases: []);

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DisplayName");
    }

    [Fact]
    public void Validate_DisplayNameTooLong_IsInvalid()
    {
        // Arrange
        string longName = new('A', 201);
        var command = new CreateClientCommand(
            DisplayName: longName,
            Aliases: []);

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DisplayName");
    }

    [Fact]
    public void Validate_EmptyAliases_IsValid()
    {
        // Arrange — aliases can be empty
        var command = new CreateClientCommand(
            DisplayName: "Test Client",
            Aliases: []);

        // Act
        ValidationResult result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
