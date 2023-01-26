using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Implementations;

/// <summary>
/// Returns a the root activity for a given <see cref="WorkflowDefinitionActivity"/>.
/// </summary>
public class WorkflowDefinitionActivityPortResolver : IActivityPortResolver
{
    private readonly IWorkflowMaterializer _materializer;
    private readonly IWorkflowDefinitionStore _store;

    public WorkflowDefinitionActivityPortResolver(IWorkflowMaterializer materializer, IWorkflowDefinitionStore store)
    {
        _materializer = materializer;
        _store = store;
    }

    public int Priority => 0;
    public bool GetSupportsActivity(IActivity activity) => activity is WorkflowDefinitionActivity;

    public async ValueTask<IEnumerable<IActivity>> GetPortsAsync(IActivity activity, CancellationToken cancellationToken = default)
    {
        var definitionActivity = (WorkflowDefinitionActivity)activity;
        var workflowDefinition = await _store.FindPublishedByDefinitionIdAsync(definitionActivity.WorkflowDefinitionId, cancellationToken);
        
        var root = await _materializer.MaterializeAsync(workflowDefinition!, cancellationToken);

        return new[] { root };
    }
}