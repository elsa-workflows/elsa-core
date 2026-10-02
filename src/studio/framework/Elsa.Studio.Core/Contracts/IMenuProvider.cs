using Elsa.Studio.Models;

namespace Elsa.Studio.Contracts;

/// Provides menu items to the dashboard.
public interface IMenuProvider
{
    /// Returns a list of menu items.
    ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default);
}