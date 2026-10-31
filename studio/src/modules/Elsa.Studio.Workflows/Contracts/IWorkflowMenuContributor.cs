using Elsa.Studio.Models;

namespace Elsa.Studio.Workflows.Contracts;

/// <summary>
/// Allows an optional module to contribute entries under the Workflows menu.
/// </summary>
public interface IWorkflowMenuContributor
{
    ValueTask<IEnumerable<MenuItem>> GetWorkflowMenuItemsAsync(CancellationToken cancellationToken = default);
}
