using FluentAssertions;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Freddy.Application.Tests.Features.Chat.Commands;

public sealed class ClientDetectorTests
{
    private readonly IClientRepository _clientRepository = Substitute.For<IClientRepository>();
    private readonly ClientDetector _detector;

    public ClientDetectorTests()
    {
        _detector = new ClientDetector(
            _clientRepository,
            NullLogger<ClientDetector>.Instance);
    }

    private static Client CreateClient(string displayName, params string[] aliases) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            DisplayName = displayName,
            Aliases = [.. aliases],
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    [Fact]
    public async Task DetectAsync_DisplayNameInMessage_ReturnsMatch()
    {
        Client client = CreateClient("Jan de Vries", "jan");
        _clientRepository.GetAllAsync(isActive: true, search: null, Arg.Any<CancellationToken>())
            .Returns([client]);

        ClientDetectionResult result = await _detector.DetectAsync(
            "Ik zoek het plan van Jan de Vries", CancellationToken.None);

        result.IsDetected.Should().BeTrue();
        result.ClientId.Should().Be(client.Id);
        result.MatchedName.Should().Be("Jan de Vries");
    }

    [Fact]
    public async Task DetectAsync_AliasInMessage_ReturnsMatch()
    {
        Client client = CreateClient("Jan de Vries", "jansen", "vries");
        _clientRepository.GetAllAsync(isActive: true, search: null, Arg.Any<CancellationToken>())
            .Returns([client]);

        ClientDetectionResult result = await _detector.DetectAsync(
            "Wat is er voor jansen beschikbaar?", CancellationToken.None);

        result.IsDetected.Should().BeTrue();
        result.ClientId.Should().Be(client.Id);
        result.MatchedName.Should().Be("jansen");
    }

    [Fact]
    public async Task DetectAsync_NoMatch_ReturnsNoMatch()
    {
        Client client = CreateClient("Jan de Vries", "jansen");
        _clientRepository.GetAllAsync(isActive: true, search: null, Arg.Any<CancellationToken>())
            .Returns([client]);

        ClientDetectionResult result = await _detector.DetectAsync(
            "Hoe werkt de voedselbank?", CancellationToken.None);

        result.IsDetected.Should().BeFalse();
        result.ClientId.Should().BeNull();
    }

    [Fact]
    public async Task DetectAsync_ShortAlias_IsIgnored()
    {
        // Aliases shorter than 3 characters should be skipped
        Client client = CreateClient("AB Client", "ab");
        _clientRepository.GetAllAsync(isActive: true, search: null, Arg.Any<CancellationToken>())
            .Returns([client]);

        ClientDetectionResult result = await _detector.DetectAsync(
            "Zoek iets voor ab", CancellationToken.None);

        // "AB Client" display name won't match "zoek iets voor ab" and alias "ab" is < 3 chars
        result.IsDetected.Should().BeFalse();
    }

    [Fact]
    public async Task DetectAsync_CaseInsensitive_DisplayName()
    {
        Client client = CreateClient("Pieter Janssen");
        _clientRepository.GetAllAsync(isActive: true, search: null, Arg.Any<CancellationToken>())
            .Returns([client]);

        ClientDetectionResult result = await _detector.DetectAsync(
            "PIETER JANSSEN zoekt hulp", CancellationToken.None);

        result.IsDetected.Should().BeTrue();
        result.ClientId.Should().Be(client.Id);
    }

    [Fact]
    public async Task DetectAsync_CaseInsensitive_Alias()
    {
        Client client = CreateClient("Someone", "pietje");
        _clientRepository.GetAllAsync(isActive: true, search: null, Arg.Any<CancellationToken>())
            .Returns([client]);

        ClientDetectionResult result = await _detector.DetectAsync(
            "Help PIETJE met voedselbank", CancellationToken.None);

        result.IsDetected.Should().BeTrue();
        result.ClientId.Should().Be(client.Id);
    }

    [Fact]
    public async Task DetectAsync_EmptyMessage_ReturnsNoMatch()
    {
        ClientDetectionResult result = await _detector.DetectAsync("", CancellationToken.None);

        result.IsDetected.Should().BeFalse();
    }

    [Fact]
    public async Task DetectAsync_WhitespaceMessage_ReturnsNoMatch()
    {
        ClientDetectionResult result = await _detector.DetectAsync("   ", CancellationToken.None);

        result.IsDetected.Should().BeFalse();
    }

    [Fact]
    public async Task DetectAsync_LongestDisplayNameMatchesFirst()
    {
        // When multiple clients could match, the longest display name wins
        Client shortName = CreateClient("Jan");
        Client longName = CreateClient("Jan de Vries");
        _clientRepository.GetAllAsync(isActive: true, search: null, Arg.Any<CancellationToken>())
            .Returns([shortName, longName]);

        ClientDetectionResult result = await _detector.DetectAsync(
            "Informatie voor Jan de Vries graag", CancellationToken.None);

        result.IsDetected.Should().BeTrue();
        result.ClientId.Should().Be(longName.Id);
        result.MatchedName.Should().Be("Jan de Vries");
    }

    [Fact]
    public async Task DetectAsync_NoClients_ReturnsNoMatch()
    {
        _clientRepository.GetAllAsync(isActive: true, search: null, Arg.Any<CancellationToken>())
            .Returns([]);

        ClientDetectionResult result = await _detector.DetectAsync(
            "Een willekeurige vraag", CancellationToken.None);

        result.IsDetected.Should().BeFalse();
    }
}
