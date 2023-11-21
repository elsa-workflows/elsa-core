namespace Elsa.Http.Contracts;

/// <summary>
/// Updates the route table based on current workflow triggers and bookmarks.
/// </summary>
public interface IRouteTableUpdater
{
    /// <summary>
    /// Updates the route table.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateAsync(CancellationToken cancellationToken = default);
}