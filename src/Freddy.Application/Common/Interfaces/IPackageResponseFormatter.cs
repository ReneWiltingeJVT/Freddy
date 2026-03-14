using Freddy.Application.Entities;

namespace Freddy.Application.Common.Interfaces;

/// <summary>
/// Formats a matched package into a user-visible Dutch response.
/// This is the primary response path for high-confidence package matches — no LLM required.
/// Runs deterministically in less than 5ms.
/// </summary>
public interface IPackageResponseFormatter
{
    /// <summary>
    /// Builds a structured Dutch response from the package title, description, and content.
    /// </summary>
    string Format(Package package);
}
