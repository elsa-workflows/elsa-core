using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Server.Web;

public class OnMyEventWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Version = 1;
        builder.Id = "OnMyEventWorkflow";
        builder.Root = new Sequence
        {
            Activities =
            {
                new Event("MyEvent")
                {
                    CanStartWorkflow = true
                },
                new Inline(async () =>
                {
                    // IEventPublisher.PublishAsync returns before this executes
                    await SomeCallAsync();
                }),
                new WriteLine("End of workflow"),
                new Finish()
            }
        };
    }
    
    private async Task SomeCallAsync()
    {
        Console.WriteLine("Hello from OnMyEventWorkflow");
        await Task.Delay(1000);
        Console.WriteLine("Goodbye from OnMyEventWorkflow");
    }
}