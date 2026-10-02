using Elsa.Studio.Models;

namespace Elsa.Studio.Security.Contracts;

/// <summary>Contributes permission-filtered children to the single Security navigation parent.</summary>
public interface ISecurityMenuContributor
{
    ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default);
}
