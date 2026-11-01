namespace Elsa.Workflows.Runtime;

public interface IBookmarkQueuePurger
{
    /// <summary>
    /// Purges expired bookmark queue items and dead letters.
    /// </summary>
    /// <remarks>
    /// The recurring purge task is <c>[RunOnceForAllTenants]</c>, so this method is called once per cycle under a single tenant context and must purge all tenants itself.
    /// </remarks>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PurgeAsync(CancellationToken cancellationToken = default);
}