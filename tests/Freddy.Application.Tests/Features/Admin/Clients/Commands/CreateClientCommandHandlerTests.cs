using FluentAssertions;
using Freddy.Application.Common;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Application.Features.Admin.Clients.Commands;
using Freddy.Application.Features.Admin.Clients.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Freddy.Application.Tests.Features.Admin.Clients.Commands;

public sealed class CreateClientCommandHandlerTests
{
    private readonly IClientRepository _clientRepository = Substitute.For<IClientRepository>();
    private readonly CreateClientCommandHandler _handler;

    public CreateClientCommandHandlerTests()
    {
        _clientRepository.CreateAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Client>());

        _handler = new CreateClientCommandHandler(
            _clientRepository,
            NullLogger<CreateClientCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsClientDto()
    {
        // Arrange
        var command = new CreateClientCommand(
            DisplayName: "Jan de Vries",
            Aliases: ["jan", "jansen"]);

        // Act
        Result<ClientDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.DisplayName.Should().Be("Jan de Vries");
        result.Value.Aliases.Should().Contain("jan");
        result.Value.Aliases.Should().Contain("jansen");
        result.Value.IsActive.Should().BeTrue();
        result.Value.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_TrimsDisplayNameAndAliases()
    {
        // Arrange
        var command = new CreateClientCommand(
            DisplayName: "  Jan de Vries  ",
            Aliases: ["  jan  ", "  jansen  "]);

        // Act
        Result<ClientDto> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.DisplayName.Should().Be("Jan de Vries");
        result.Value.Aliases.Should().AllSatisfy(a => a.Should().NotStartWith(" ").And.NotEndWith(" "));
    }

    [Fact]
    public async Task Handle_CallsRepositoryCreate()
    {
        // Arrange
        var command = new CreateClientCommand(
            DisplayName: "Test",
            Aliases: []);

        // Act
        _ = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _clientRepository.Received(1)
            .CreateAsync(Arg.Is<Client>(c => c.DisplayName == "Test"), Arg.Any<CancellationToken>());
    }
}
