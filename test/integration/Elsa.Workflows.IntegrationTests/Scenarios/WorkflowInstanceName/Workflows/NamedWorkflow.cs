using Elsa.Extensions;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowInstanceName.Workflows;

public class NamedWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new WriteLine(x => x.GetWorkflowExecutionContext().Name);
    }
}