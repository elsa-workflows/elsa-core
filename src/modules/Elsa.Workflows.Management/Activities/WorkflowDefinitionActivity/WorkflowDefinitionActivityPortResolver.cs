using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;

/// <summary>
/// Returns the root activity for a given <see cref="WorkflowDefinitionActivity"/>.
/// </summary>
public class WorkflowDefinitionActivityPortResolver : IActivityPortResolver
{
    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public bool GetSupportsActivity(IActivity activity) => activity is WorkflowDefinitionActivity;

    /// <inheritdoc />
    public ValueTask<IEnumerable<IActivity>> GetPortsAsync(IActivity activity, CancellationToken cancellationToken = default)
    {
        var definitionActivity = (WorkflowDefinitionActivity)activity;
        var root = definitionActivity.Root;

        return new(new[] { root });
    }
}