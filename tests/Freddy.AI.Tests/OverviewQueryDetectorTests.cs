using FluentAssertions;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Freddy.AI.Tests;

public sealed class OverviewQueryDetectorTests
{
    private readonly OverviewQueryDetector _detector = new(NullLogger<OverviewQueryDetector>.Instance);

    // ── Count queries ────────────────────────────────────────────────────

    [Theory]
    [InlineData("hoeveel protocollen zijn er?")]
    [InlineData("What is the aantal protocollen?")]
    [InlineData("hoeveel protocol heb je?")]
    public void Detect_CountProtocol_ReturnsCountByCategory(string input)
    {
        OverviewQueryIntent result = _detector.Detect(input);

        result.IsOverview.Should().BeTrue();
        result.QueryType.Should().Be(OverviewQueryType.CountByCategory);
        result.Category.Should().Be(PackageCategory.Protocol);
    }

    [Theory]
    [InlineData("hoeveel werkinstructies zijn er?")]
    [InlineData("aantal werkinstructies beschikbaar?")]
    public void Detect_CountWorkInstruction_ReturnsCountByCategory(string input)
    {
        OverviewQueryIntent result = _detector.Detect(input);

        result.IsOverview.Should().BeTrue();
        result.QueryType.Should().Be(OverviewQueryType.CountByCategory);
        result.Category.Should().Be(PackageCategory.WorkInstruction);
    }

    // ── List queries ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("welke protocollen zijn er?")]
    [InlineData("welke protocollen zijn beschikbaar")]
    [InlineData("geef me alle protocollen")]
    public void Detect_ListProtocol_ReturnsListByCategory(string input)
    {
        OverviewQueryIntent result = _detector.Detect(input);

        result.IsOverview.Should().BeTrue();
        result.QueryType.Should().Be(OverviewQueryType.ListByCategory);
        result.Category.Should().Be(PackageCategory.Protocol);
    }

    [Theory]
    [InlineData("welke werkinstructies zijn er?")]
    [InlineData("toon alle werkinstructies")]
    public void Detect_ListWorkInstruction_ReturnsListByCategory(string input)
    {
        OverviewQueryIntent result = _detector.Detect(input);

        result.IsOverview.Should().BeTrue();
        result.QueryType.Should().Be(OverviewQueryType.ListByCategory);
        result.Category.Should().Be(PackageCategory.WorkInstruction);
    }

    [Theory]
    [InlineData("welke pakketten zijn er?")]
    [InlineData("welke pakketten zijn beschikbaar?")]
    [InlineData("geef me alle pakketten")]
    public void Detect_ListAll_ReturnsListAll(string input)
    {
        OverviewQueryIntent result = _detector.Detect(input);

        result.IsOverview.Should().BeTrue();
        result.QueryType.Should().Be(OverviewQueryType.ListAll);
    }

    // ── Personal plans for client ─────────────────────────────────────────

    [Theory]
    [InlineData("welke plannen zijn er voor meneer van het Hout?")]
    [InlineData("welke plannen zijn er voor mevrouw Jansen?")]
    public void Detect_PlansForClient_ReturnsPersonalPlansForClient(string input)
    {
        OverviewQueryIntent result = _detector.Detect(input);

        result.IsOverview.Should().BeTrue();
        result.QueryType.Should().Be(OverviewQueryType.PersonalPlansForClient);
        result.Category.Should().Be(PackageCategory.PersonalPlan);
        result.ClientNameHint.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Detect_PlansForClientVanHetHout_ExtractsNameHint()
    {
        OverviewQueryIntent result = _detector.Detect("welke plannen zijn er voor meneer van het Hout?");

        result.ClientNameHint.Should().Contain("Hout");
    }

    // ── Existence / availability queries ─────────────────────────────────

    [Theory]
    [InlineData("zijn er protocollen beschikbaar?")]
    [InlineData("zijn er nog protocollen beschikbaar?")]
    [InlineData("is er een protocol?")]
    [InlineData("bestaat er een protocol?")]
    [InlineData("hebben jullie protocollen?")]
    [InlineData("heb je een protocol?")]
    public void Detect_ExistenceQueryProtocol_ReturnsListByCategory(string input)
    {
        OverviewQueryIntent result = _detector.Detect(input);

        result.IsOverview.Should().BeTrue();
        result.QueryType.Should().Be(OverviewQueryType.ListByCategory);
        result.Category.Should().Be(PackageCategory.Protocol);
    }

    [Theory]
    [InlineData("zijn er werkinstructies beschikbaar?")]
    [InlineData("hebben jullie werkinstructies?")]
    [InlineData("is er een werkinstructie?")]
    public void Detect_ExistenceQueryWorkInstruction_ReturnsListByCategory(string input)
    {
        OverviewQueryIntent result = _detector.Detect(input);

        result.IsOverview.Should().BeTrue();
        result.QueryType.Should().Be(OverviewQueryType.ListByCategory);
        result.Category.Should().Be(PackageCategory.WorkInstruction);
    }

    [Theory]
    [InlineData("zijn er pakketten beschikbaar?")]
    [InlineData("heb je een pakket?")]
    [InlineData("bestaan er pakketten?")]
    public void Detect_ExistenceQueryPackage_ReturnsListAll(string input)
    {
        OverviewQueryIntent result = _detector.Detect(input);

        result.IsOverview.Should().BeTrue();
        result.QueryType.Should().Be(OverviewQueryType.ListAll);
    }

    // ── Non-overview messages → None ──────────────────────────────────────

    [Theory]
    [InlineData("hoi")]
    [InlineData("ik heb een vraag over wassen")]
    [InlineData("hoe vraag ik een voedselpakket aan?")]
    [InlineData("help me met het wasprotocol")]
    [InlineData("wat is het protocol voor medicatie?")]
    public void Detect_NonOverview_ReturnsNone(string input)
    {
        OverviewQueryIntent result = _detector.Detect(input);

        result.IsOverview.Should().BeFalse();
        result.QueryType.Should().Be(OverviewQueryType.None);
    }

    [Fact]
    public void Detect_EmptyMessage_ReturnsNone()
    {
        OverviewQueryIntent result = _detector.Detect(string.Empty);

        result.IsOverview.Should().BeFalse();
    }

    [Fact]
    public void Detect_WhitespaceMessage_ReturnsNone()
    {
        OverviewQueryIntent result = _detector.Detect("   ");

        result.IsOverview.Should().BeFalse();
    }
}
