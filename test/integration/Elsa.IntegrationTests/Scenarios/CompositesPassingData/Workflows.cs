using Elsa.Extensions;
using Elsa.Workflows.Core.Abstractions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Activities.SetOutput;

namespace Elsa.IntegrationTests.Scenarios.CompositesPassingData;

/// <summary>
/// A composite activity that adds text to a string
/// </summary>
public class AddTextSubWorkflow : Composite
{
    public Input<string> A { get; set; } = default!;
    public Output<string> B { get; set; } = default!;

    public AddTextSubWorkflow()
    {
        var setOutput = new SetOutput()
        {
            OutputName = new Input<string>("B"),
            OutputValue = new Input<object?>(context => "hi there " + A.Get(context))
        };

        Root = new Sequence
        {
            Activities =
            {
                setOutput
            }
        };
    }
}

/// <summary>
/// A workflow that uses the <see cref="AddTextSubWorkflow"/> composite activity.
/// </summary>
public class AddTextMainWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var outVariable = new Variable<string>("out", "test");

        var subWorkflow = new AddTextSubWorkflow()
        {
            A = new("obi wan"),
            B = new (outVariable)
        };

        workflow.Variables.Add(outVariable);

        workflow.Root = new Sequence
        {
            Activities =
            {
                subWorkflow,
                new WriteLine(context => outVariable.Get(context))
             }
        };
    }
}