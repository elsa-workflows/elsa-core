using System.Text;
using Elsa.Workflows.Admission;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Slack.SocketMode.Persistence;

internal sealed record SlackSocketReceiptCleanupBatch(int Examined, int Removed, bool ScanCompleted);

/// <summary>
/// One finite page per scheduled call, with an exclusive cursor retained across calls. Bind one
/// driver per distinct trusted cleanup authority so alternating authorities cannot starve rows.
/// </summary>
internal sealed class SlackSocketReceiptCleanupDriver
{
    private readonly IServiceScopeFactory _scopes;
    private readonly string _authority;
    private string? _cursor;
    private int _running;

    internal SlackSocketReceiptCleanupDriver(IServiceScopeFactory scopes, string authority)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        if (string.IsNullOrWhiteSpace(authority) || Encoding.UTF8.GetByteCount(authority) > AdmissionLimits.AuthorityBytes)
        {
            throw new ArgumentException("A bounded explicit receipt cleanup authority is required.", nameof(authority));
        }
        _scopes = scopes;
        _authority = authority;
    }

    internal async Task<SlackSocketReceiptCleanupBatch> RunBatchAsync(int limit, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            throw new InvalidOperationException("A receipt cleanup batch is already running.");
        }
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<ISlackSocketDiscardStore>();
            var page = await store.FindCleanupCandidatesAsync(limit, _cursor, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (page.Count > limit)
            {
                throw new InvalidOperationException("Receipt cleanup exceeded its bounded page.");
            }
            var removed = 0;
            foreach (var candidate in page)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await store.CleanupAsync(candidate.Id, candidate.Revision, now, _authority, cancellationToken))
                {
                    removed++;
                }
                // Ineligible/stale rows must not pin the first page. An uncertain cleanup throws
                // before advancing this candidate; retry remains safe under the store's CAS/lock.
                _cursor = candidate.Id;
            }
            var completed = page.Count < limit;
            if (completed)
            {
                // A later call restarts the scan, including insertions before the old cursor.
                _cursor = null;
            }
            return new(page.Count, removed, completed);
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }
}
