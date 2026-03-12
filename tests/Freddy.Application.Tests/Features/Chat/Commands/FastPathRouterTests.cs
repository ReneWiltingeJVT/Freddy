using FluentAssertions;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;
using Freddy.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Freddy.Application.Tests.Features.Chat.Commands;

public sealed class FastPathRouterTests
{
    private readonly FastPathRouter _router = new(NullLogger<FastPathRouter>.Instance);

    private static PackageCandidate CreateCandidate(
        string title,
        string description = "Beschrijving",
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<string>? synonyms = null) =>
        new(
            Guid.CreateVersion7(),
            title,
            description,
            tags ?? [],
            synonyms ?? []);

    [Fact]
    public void Score_ExactTitleMatch_Returns1()
    {
        // Arrange
        PackageCandidate candidate = CreateCandidate("Voedselbank");

        // Act
        IReadOnlyList<ScoredCandidate> results = _router.Score("Voedselbank", [candidate]);

        // Assert
        results.Should().HaveCount(1);
        results[0].Score.Should().Be(1.0);
    }

    [Fact]
    public void Score_TitleContainedInMessage_Returns07()
    {
        // Arrange
        PackageCandidate candidate = CreateCandidate("Voedselbank");

        // Act
        IReadOnlyList<ScoredCandidate> results = _router.Score("Hoe werkt de voedselbank?", [candidate]);

        // Assert
        results.Should().HaveCount(1);
        results[0].Score.Should().Be(0.7);
    }

    [Fact]
    public void Score_ExactTagMatch_Returns06()
    {
        // Arrange
        PackageCandidate candidate = CreateCandidate(
            "Voedselbank",
            tags: ["voedselpakket", "hulpverlening"]);

        // Act
        IReadOnlyList<ScoredCandidate> results = _router.Score("Hoe vraag ik een voedselpakket aan?", [candidate]);

        // Assert
        results.Should().HaveCount(1);
        results[0].Score.Should().BeGreaterThanOrEqualTo(0.6);
    }

    [Fact]
    public void Score_ExactSynonymMatch_Returns06()
    {
        // Arrange
        PackageCandidate candidate = CreateCandidate(
            "Voedselbank",
            synonyms: ["voedselbank", "voedselpakketten"]);

        // Act
        IReadOnlyList<ScoredCandidate> results = _router.Score("Waar kan ik voedselpakketten ophalen?", [candidate]);

        // Assert
        results.Should().HaveCount(1);
        results[0].Score.Should().BeGreaterThanOrEqualTo(0.6);
    }

    [Fact]
    public void Score_PartialTagMatch_Returns03()
    {
        // Arrange
        PackageCandidate candidate = CreateCandidate(
            "Medicatie in Beheer",
            tags: ["medicatieveiligheid"]);

        // Act
        IReadOnlyList<ScoredCandidate> results = _router.Score("Is er iets over medicatie?", [candidate]);

        // Assert
        results.Should().HaveCount(1);
        results[0].Score.Should().BeGreaterThanOrEqualTo(0.3);
    }

    [Fact]
    public void Score_NoMatch_ReturnsEmpty()
    {
        // Arrange
        PackageCandidate candidate = CreateCandidate(
            "Voedselbank",
            tags: ["voedselpakket"],
            synonyms: ["voedselbank"]);

        // Act
        IReadOnlyList<ScoredCandidate> results = _router.Score("Wat is het weer vandaag?", [candidate]);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Score_MultipleCandidates_OrderedByScoreDescending()
    {
        // Arrange
        PackageCandidate voedselbank = CreateCandidate(
            "Voedselbank",
            tags: ["voedselpakket"],
            synonyms: ["voedselbank"]);

        PackageCandidate medicatie = CreateCandidate(
            "Medicatie in Beheer",
            tags: ["medicatie"],
            synonyms: ["medicijnen"]);

        // Act — message matches voedselbank better
        IReadOnlyList<ScoredCandidate> results = _router.Score(
            "Hoe werkt de voedselbank?",
            [voedselbank, medicatie]);

        // Assert
        results.Should().HaveCountGreaterThan(0);
        results[0].Candidate.Title.Should().Be("Voedselbank");
    }

    [Fact]
    public void Score_EmptyCandidates_ReturnsEmpty()
    {
        IReadOnlyList<ScoredCandidate> results = _router.Score("Test", []);
        results.Should().BeEmpty();
    }

    [Fact]
    public void Score_CaseInsensitiveMatching()
    {
        // Arrange
        PackageCandidate candidate = CreateCandidate(
            "Valpreventie",
            tags: ["valrisico"],
            synonyms: ["Valpreventie"]);

        // Act
        IReadOnlyList<ScoredCandidate> results = _router.Score("VALPREVENTIE informatie", [candidate]);

        // Assert
        results.Should().HaveCount(1);
        results[0].Score.Should().BeGreaterThanOrEqualTo(0.6);
    }

    [Fact]
    public void Score_DescriptionOverlap_Returns02()
    {
        // Arrange
        PackageCandidate candidate = CreateCandidate(
            "XYZ Protocol",
            description: "Protocol voor het voorbereiden en organiseren van activiteiten",
            tags: [],
            synonyms: []);

        // Act — shares "voorbereiden" and "organiseren" with description (≥2 overlap words)
        IReadOnlyList<ScoredCandidate> results = _router.Score(
            "Help met het voorbereiden en organiseren",
            [candidate]);

        // Assert — description overlap raised to 0.3 after scoring improvements
        results.Should().HaveCount(1);
        results[0].Score.Should().Be(0.3);
    }

    [Fact]
    public void Score_RealWorldSeedData_VoedselbankvraagMatchesBest()
    {
        // Arrange — mirrors actual seed data
        PackageCandidate voedselbank = CreateCandidate(
            "Voedselbank",
            description: "Protocol voor het aanvragen, samenstellen en distribueren van voedselbankpakketten",
            tags: ["voedselpakket", "voedselbank", "pakket samenstellen", "dieetbeperkingen", "allergieën", "distributie", "hulpverlening"],
            synonyms: ["voedselpakketten", "voedselbank", "voedselbankpakket", "voedselhulp"]);

        PackageCandidate medicatie = CreateCandidate(
            "Medicatie in Beheer",
            description: "Protocol voor verantwoord medicatiebeheer in de instelling",
            tags: ["medicatie", "medicijnen", "bijwerkingen", "apotheek", "medicatieveiligheid", "voorraad"],
            synonyms: ["medicatiebeheer", "medicijnen", "pillen", "medicatie-afgifte", "geneesmiddelen"]);

        PackageCandidate valpreventie = CreateCandidate(
            "Valpreventie",
            description: "Protocol voor het voorkomen van valincidenten",
            tags: ["valrisico", "valpreventiebeleid", "hulpmiddelen", "valincident", "bewegingsbeperking", "fysieke veiligheid"],
            synonyms: ["valpreventie", "valgevaar", "valrisicobeoordeling", "anti-valbeleid", "valprotocol", "veiligheid bij vallen"]);

        // Act
        IReadOnlyList<ScoredCandidate> results = _router.Score(
            "Hoe vraag ik een voedselpakket aan voor een bewoner?",
            [voedselbank, medicatie, valpreventie]);

        // Assert — voedselbank should score highest
        results.Should().HaveCountGreaterThan(0);
        results[0].Candidate.Title.Should().Be("Voedselbank");
        results[0].Score.Should().BeGreaterThanOrEqualTo(0.6);
    }

    [Fact]
    public void Score_PersonalPlanCategory_ReceivesCategoryBoost()
    {
        // Arrange — two identical candidates, one PersonalPlan
        PackageCandidate protocol = new(
            Guid.CreateVersion7(), "Medicatiebeheer", "Protocol voor medicatie",
            ["medicatie"], ["medicijnen"], "", null, PackageCategory.Protocol);

        PackageCandidate personalPlan = new(
            Guid.CreateVersion7(), "Medicatiebeheer", "Protocol voor medicatie",
            ["medicatie"], ["medicijnen"], "", null, PackageCategory.PersonalPlan);

        // Act
        IReadOnlyList<ScoredCandidate> protocolResults = _router.Score("medicatie", [protocol]);
        IReadOnlyList<ScoredCandidate> personalResults = _router.Score("medicatie", [personalPlan]);

        // Assert — PersonalPlan should score 0.1 higher due to category boost
        protocolResults.Should().HaveCount(1);
        personalResults.Should().HaveCount(1);
        personalResults[0].Score.Should().Be(protocolResults[0].Score + 0.1);
    }

    [Fact]
    public void Score_PersonalPlanCategory_BoostCappedAtOne()
    {
        // Arrange — PersonalPlan with exact title match (1.0) should stay at 1.0
        PackageCandidate personalPlan = new(
            Guid.CreateVersion7(), "Voedselbank", "Beschrijving",
            [], [], "", null, PackageCategory.PersonalPlan);

        // Act
        IReadOnlyList<ScoredCandidate> results = _router.Score("Voedselbank", [personalPlan]);

        // Assert — capped at 1.0 despite +0.1 boost
        results.Should().HaveCount(1);
        results[0].Score.Should().Be(1.0);
    }

    [Fact]
    public void Score_PersonalPlanCategory_NoBoostWhenZeroScore()
    {
        // Arrange — PersonalPlan that doesn't match at all should remain absent
        PackageCandidate personalPlan = new(
            Guid.CreateVersion7(), "XYZ Plan", "Onbekend onderwerp",
            [], [], "", null, PackageCategory.PersonalPlan);

        // Act
        IReadOnlyList<ScoredCandidate> results = _router.Score("Wat is het weer vandaag?", [personalPlan]);

        // Assert — no match means no boost
        results.Should().BeEmpty();
    }
}
