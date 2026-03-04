namespace Freddy.Application.Entities;

/// <summary>
/// Tracks what the assistant is currently waiting for the user to respond to.
/// </summary>
public enum ConversationPendingState
{
    /// <summary>No pending interaction — normal routing applies.</summary>
    None = 0,

    /// <summary>
    /// The assistant asked the user to confirm a package match.
    /// PendingPackageId holds the candidate package.
    /// </summary>
    AwaitingPackageConfirmation = 1,

    /// <summary>
    /// Package content has been delivered; the assistant asked whether the user
    /// wants to receive the associated documents.
    /// PendingPackageId holds the package whose documents to return.
    /// </summary>
    AwaitingDocumentDelivery = 2,
}
