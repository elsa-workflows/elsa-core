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
    private readonly IWorkflowDefinitionActivityMaterializer _materializer;

    public WorkflowDefinitionActivityPortResolver(IWorkflowDefinitionActivityMaterializer materializer)
    {
        _materializer = materializer;
    }

    public int Priority => 0;
    public bool GetSupportsActivity(IActivity activity) => activity is WorkflowDefinitionActivity;

    public async ValueTask<IEnumerable<IActivity>> GetPortsAsync(IActivity activity, CancellationToken cancellationToken = default)
    {
        var root = await _materializer.MaterializeAsync((WorkflowDefinitionActivity)activity, cancellationToken);

        return new[] { root };
    }
}