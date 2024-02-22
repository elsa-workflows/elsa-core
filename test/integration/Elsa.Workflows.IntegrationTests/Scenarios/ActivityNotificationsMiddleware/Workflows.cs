using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ActivityNotificationsMiddleware;

class HelloWorldWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new WriteLine("Hello World!");
    }
}