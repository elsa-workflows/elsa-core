using Elsa.Workflows;

namespace Elsa.Testing.Shared;

public class TestWorkflow : WorkflowBase
{
    private readonly Action<IWorkflowBuilder> _buildWorkflow;

    public TestWorkflow(Action<IWorkflowBuilder> buildWorkflow)
    {
        _buildWorkflow = buildWorkflow;
    }

    protected override void Build(IWorkflowBuilder workflowBuilder)
    {
        _buildWorkflow(workflowBuilder);

        if (string.IsNullOrEmpty(workflowBuilder.Id))
        {
            workflowBuilder.Id = Guid.NewGuid().ToString();
        }
    }
}