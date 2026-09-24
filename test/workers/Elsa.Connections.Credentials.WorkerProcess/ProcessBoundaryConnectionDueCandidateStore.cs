using Elsa.Connections.Contracts;
using Elsa.Connections.Models;

namespace Elsa.Connections.Credentials.WorkerProcess;

internal sealed class ProcessBoundaryConnectionDueCandidateStore(IConnectionDueCandidateStore inner) : IConnectionDueCandidateStore
{
    public async Task<ConnectionDueCandidatePage> FindDueCandidatesAsync(
        string tenantId,
        string environmentId,
        DateTimeOffset now,
        int pageSize,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var page = await inner.FindDueCandidatesAsync(tenantId, environmentId, now, pageSize, cursor, cancellationToken);
        // The page count and scope-free boundary are test synchronization only; never write candidate IDs or payloads.
        Console.WriteLine($"DUE_PAGE:{page.Items.Count}");
        await Console.Out.FlushAsync();
        return page;
    }
}
