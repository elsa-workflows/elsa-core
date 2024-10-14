using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;

/// <summary>
/// Returns the root activity for a given <see cref="WorkflowDefinitionActivity"/>.
/// </summary>
public class WorkflowDefinitionActivityResolver : IActivityResolver
{
    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public bool GetSupportsActivity(IActivity activity) => activity is WorkflowDefinitionActivity;

    /// <inheritdoc />
    public ValueTask<IEnumerable<ActivityPort>> GetActivityPortsAsync(IActivity activity, CancellationToken cancellationToken = default)
    {
        var definitionActivity = (WorkflowDefinitionActivity)activity;
        var root = definitionActivity.Root;
        var port = ActivityPort.FromActivity(root, nameof(WorkflowDefinitionActivity.Root));

        return new([port]);
    }
}