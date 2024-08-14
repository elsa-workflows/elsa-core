using Elsa.Workflows.Runtime;

namespace Elsa.Workflows.ComponentTests.Helpers;

public class TestWorkflowProvider : IWorkflowsProvider
{
    public ICollection<MaterializedWorkflow> MaterializedWorkflows { get; set; } = new List<MaterializedWorkflow>();
    public string Name => "Test";

    public ValueTask<IEnumerable<MaterializedWorkflow>> GetWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        return new(MaterializedWorkflows);
    }
}