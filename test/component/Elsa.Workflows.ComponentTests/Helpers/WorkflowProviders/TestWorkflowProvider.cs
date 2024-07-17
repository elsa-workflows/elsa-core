using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.ComponentTests.Helpers.WorkflowProviders;

public class TestWorkflowProvider : IWorkflowProvider
{
    public string Name => "Test";
    public ICollection<MaterializedWorkflow> MaterializedWorkflows { get; set; } = new List<MaterializedWorkflow>();
    public ValueTask<IEnumerable<MaterializedWorkflow>> GetWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        return new(MaterializedWorkflows);
    }
}