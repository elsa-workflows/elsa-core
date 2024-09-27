using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Server.Web;

public class SampleWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        // The WithVariable method ensures that the created variable will be added to the Workflow's Variables collection, which is required for persistent variables.
        var variable1 = workflow.WithVariable<string>("Foo").WithWorkflowStorage();
        
        workflow.Variables =
        [
            variable1
        ];

        workflow.Root = new Sequence
        {
            Activities =
            {
                new StartAt(DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5))
                {
                    CanStartWorkflow = true
                },
                new WriteLine(variable1),
                new SetVariable
                {
                    Variable = variable1,
                    Value = new (Literal.From("Bar"))
                },
                new Delay(TimeSpan.FromSeconds(1)),
                new WriteLine(variable1)
            }
        };
    }
}