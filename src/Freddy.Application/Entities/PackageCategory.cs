namespace Freddy.Application.Entities;

/// <summary>
/// Categorises packages by their scope and intended audience.
/// </summary>
public enum PackageCategory
{
    /// <summary>General guidelines or formal care processes — not client-specific.</summary>
    Protocol = 0,

    /// <summary>Practical instructions for care workers — not client-specific.</summary>
    WorkInstruction = 1,

    /// <summary>Client-specific instructions — requires a linked Client.</summary>
    PersonalPlan = 2,
}
