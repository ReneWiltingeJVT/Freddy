namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Categories of small talk that Freddy can detect and handle with template responses.
/// </summary>
public enum SmallTalkCategory
{
    /// <summary>Not small talk — route to the normal pipeline.</summary>
    None = 0,

    /// <summary>Greeting — "Hoi", "Goedemorgen", "Hey Freddy".</summary>
    Greeting,

    /// <summary>Help intent — "Ik heb een vraag", "Kun je me helpen?".</summary>
    HelpIntent,

    /// <summary>Thanks — "Dank je", "Bedankt", "Dankjewel".</summary>
    Thanks,

    /// <summary>Farewell — "Doei", "Tot ziens", "Fijne dag".</summary>
    Farewell,

    /// <summary>Generic confusion — "Huh?", "Ik snap het niet".</summary>
    GenericConfusion,
}
