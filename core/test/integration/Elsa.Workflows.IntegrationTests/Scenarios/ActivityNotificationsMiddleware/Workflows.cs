using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ActivityNotificationsMiddleware;

class HelloWorldWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new WriteLine("Hello World!");
    }
}

class ThrowingWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new ThrowingActivity();
    }
}

class ThrowingActivity : CodeActivity
{
    public const string ErrorMessage = "thrown from activity";

    protected override void Execute(ActivityExecutionContext context) => throw new InvalidOperationException(ErrorMessage);
}
