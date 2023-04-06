using Elsa.Workflows.Core.Abstractions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.IntegrationTests.Scenarios.ImplicitJoins.Workflows;

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
                new Connection(counterGreaterThanOne, retry, SourcePort: "False"),
                new Connection(counterGreaterThanOne, end, SourcePort: "True"),
                new Connection(retry, incrementCounter),
            }
        };
    }
}