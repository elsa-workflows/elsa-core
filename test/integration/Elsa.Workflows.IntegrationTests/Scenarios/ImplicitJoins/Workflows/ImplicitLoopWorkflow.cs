using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ImplicitJoins.Workflows;

public class ImplicitLoopWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var counterVariable = builder.WithVariable(0);
        var start = new WriteLine("Start");
        var incrementCounter = new SetVariable<int>(counterVariable, context => counterVariable.Get(context) + 1);
        var counterGreaterThanOne = new FlowDecision(context => counterVariable.Get(context) > 1);
        var retry = new WriteLine("Retry");
        var end = new WriteLine("End");

        builder.Root = new Flowchart
        {
            Start = start,
            
            Activities =
            {
                start,
                incrementCounter,
                counterGreaterThanOne,
                retry,
                end
            },
            
            Connections =
            {
                new Connection(start, incrementCounter),
                new Connection(incrementCounter, counterGreaterThanOne),
                new Connection(new Endpoint(counterGreaterThanOne, "False"), new Endpoint(retry)),
                new Connection(new Endpoint(counterGreaterThanOne, "True"), new Endpoint(end)),
                new Connection(retry, incrementCounter),
            }
        };
    }
}