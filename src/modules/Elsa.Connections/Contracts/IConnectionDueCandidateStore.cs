using Elsa.Connections.Models;

namespace Elsa.Connections.Contracts;

/// <summary>Queries bounded, scoped, nonsecret pointers to due lifecycle work.</summary>
public interface IConnectionDueCandidateStore
{
    /// <summary>
    /// Returns at most <paramref name="pageSize"/> candidates due at or before <paramref name="now"/>.
    /// Null credential metadata on preexisting rows is intentionally excluded from OAuth scheduling; those rows require an explicit on-demand lifecycle operation.
    /// </summary>
    Task<ConnectionDueCandidatePage> FindDueCandidatesAsync(
        string tenantId,
        string environmentId,
        DateTimeOffset now,
        int pageSize,
        string? cursor = null,
        CancellationToken cancellationToken = default);
}

public sealed record ConnectionDueCandidatePage(IReadOnlyList<ConnectionDueCandidate> Items, string? NextCursor);
