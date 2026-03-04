using FluentAssertions;
using Freddy.Application.Common.Interfaces;
using Freddy.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Freddy.AI.Tests;

public sealed class SmallTalkDetectorTests
{
    private readonly SmallTalkDetector _detector = new(NullLogger<SmallTalkDetector>.Instance);

    [Theory]
    [InlineData("hallo")]
    [InlineData("Hallo")]
    [InlineData("hoi")]
    [InlineData("hey")]
    [InlineData("goedemorgen")]
    [InlineData("goedemiddag")]
    [InlineData("goedenavond")]
    [InlineData("hallo!")]
    [InlineData("Hoi!")]
    public void Detect_Greeting_ReturnsGreetingCategory(string input)
    {
        SmallTalkResult result = _detector.Detect(input);

        result.IsSmallTalk.Should().BeTrue();
        result.Category.Should().Be(SmallTalkCategory.Greeting);
        result.TemplateResponse.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("hallo daar")]
    [InlineData("hoi zeg")]
    [InlineData("hey freddy!")]
    public void Detect_GreetingPrefix_ReturnsGreetingCategory(string input)
    {
        SmallTalkResult result = _detector.Detect(input);

        result.IsSmallTalk.Should().BeTrue();
        result.Category.Should().Be(SmallTalkCategory.Greeting);
    }

    [Theory]
    [InlineData("help")]
    [InlineData("hulp")]
    [InlineData("kun je me helpen")]
    [InlineData("ik heb hulp nodig")]
    [InlineData("wat kan je")]
    [InlineData("wat kun je")]
    public void Detect_HelpIntent_ReturnsHelpIntentCategory(string input)
    {
        SmallTalkResult result = _detector.Detect(input);

        result.IsSmallTalk.Should().BeTrue();
        result.Category.Should().Be(SmallTalkCategory.HelpIntent);
        result.TemplateResponse.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("bedankt")]
    [InlineData("dankjewel")]
    [InlineData("dank je wel")]
    [InlineData("thanks")]
    [InlineData("merci")]
    [InlineData("top bedankt")]
    public void Detect_Thanks_ReturnsThanksCategory(string input)
    {
        SmallTalkResult result = _detector.Detect(input);

        result.IsSmallTalk.Should().BeTrue();
        result.Category.Should().Be(SmallTalkCategory.Thanks);
        result.TemplateResponse.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("doei")]
    [InlineData("tot ziens")]
    [InlineData("dag!")]
    [InlineData("tot later")]
    [InlineData("fijne dag")]
    public void Detect_Farewell_ReturnsFarewellCategory(string input)
    {
        SmallTalkResult result = _detector.Detect(input);

        result.IsSmallTalk.Should().BeTrue();
        result.Category.Should().Be(SmallTalkCategory.Farewell);
        result.TemplateResponse.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("???")]
    [InlineData("?")]
    [InlineData("huh")]
    [InlineData("wat")]
    [InlineData("snap ik niet")]
    [InlineData("ik begrijp het niet")]
    public void Detect_Confusion_ReturnsGenericConfusionCategory(string input)
    {
        SmallTalkResult result = _detector.Detect(input);

        result.IsSmallTalk.Should().BeTrue();
        result.Category.Should().Be(SmallTalkCategory.GenericConfusion);
        result.TemplateResponse.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("Hoe werkt het protocol voor medicatie?")]
    [InlineData("Wat zijn de bijwerkingen van paracetamol?")]
    [InlineData("Kun je uitleggen hoe de bloeddruk wordt gemeten?")]
    [InlineData("Ik wil meer weten over diabetes type 2")]
    [InlineData("Wat is de juiste dosering voor ibuprofen?")]
    public void Detect_RealQuestion_ReturnsNoMatch(string input)
    {
        SmallTalkResult result = _detector.Detect(input);

        result.IsSmallTalk.Should().BeFalse();
        result.Category.Should().Be(SmallTalkCategory.None);
        result.TemplateResponse.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Detect_EmptyOrWhitespace_ReturnsNoMatch(string input)
    {
        SmallTalkResult result = _detector.Detect(input);

        result.IsSmallTalk.Should().BeFalse();
        result.Category.Should().Be(SmallTalkCategory.None);
    }

    [Fact]
    public void Detect_IsCaseInsensitive()
    {
        SmallTalkResult lower = _detector.Detect("hallo");
        SmallTalkResult upper = _detector.Detect("HALLO");
        SmallTalkResult mixed = _detector.Detect("Hallo");

        lower.Category.Should().Be(SmallTalkCategory.Greeting);
        upper.Category.Should().Be(SmallTalkCategory.Greeting);
        mixed.Category.Should().Be(SmallTalkCategory.Greeting);
    }

    [Fact]
    public void Detect_StripsExtraneousPunctuation()
    {
        SmallTalkResult result = _detector.Detect("bedankt!");

        result.IsSmallTalk.Should().BeTrue();
        result.Category.Should().Be(SmallTalkCategory.Thanks);
    }

    [Theory]
    [InlineData("hallo maar ik heb een heel lang verhaal over hoe het protocol werkt bij mijn patient")]
    public void Detect_GreetingPrefixWithLongRemainder_ReturnsNoMatch(string input)
    {
        // Greeting prefix + long remainder should not match as small talk
        SmallTalkResult result = _detector.Detect(input);

        result.IsSmallTalk.Should().BeFalse();
    }
}
