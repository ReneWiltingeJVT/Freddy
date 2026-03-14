using System.Text;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Entities;

namespace Freddy.Infrastructure.AI;

/// <summary>
/// Deterministic Dutch formatter for package responses.
/// Produces structured, readable output from package data without any LLM call.
/// Used as the primary response path for high-confidence package matches.
/// </summary>
public sealed class PackageResponseFormatter : IPackageResponseFormatter
{
    /// <inheritdoc />
    public string Format(Package package)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Ik heb het volgende pakket gevonden:");
        sb.AppendLine();
        sb.Append("**");
        sb.Append(package.Title);
        sb.AppendLine("**");

        if (!string.IsNullOrWhiteSpace(package.Description))
        {
            sb.AppendLine();
            sb.AppendLine(package.Description);
        }

        if (!string.IsNullOrWhiteSpace(package.Content))
        {
            sb.AppendLine();
            sb.Append(package.Content);
        }

        return sb.ToString().TrimEnd();
    }
}
