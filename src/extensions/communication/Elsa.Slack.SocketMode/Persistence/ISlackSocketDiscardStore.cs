namespace Elsa.Slack.SocketMode.Persistence;

internal interface ISlackSocketDiscardStore
{
    Task ValidateProvisioningAsync(CancellationToken cancellationToken = default);
    Task<SlackSocketDiscardResult> RecordDiscardAsync(SlackSocketDiscardRequest request, DateTimeOffset now, CancellationToken cancellationToken = default);
    // Exclusive ID cursor. Restart a completed scan to discover later insertions.
    Task<IReadOnlyList<SlackSocketDiscardCleanupCandidate>> FindCleanupCandidatesAsync(int limit, string? afterId, CancellationToken cancellationToken = default);
    Task<bool> CleanupAsync(string receiptId, long revision, DateTimeOffset now, string authority, CancellationToken cancellationToken = default);
}
