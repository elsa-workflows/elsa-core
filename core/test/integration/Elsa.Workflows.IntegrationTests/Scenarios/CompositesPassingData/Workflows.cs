using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Activities.SetOutput;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.CompositesPassingData;

/// <summary>
/// A composite activity that adds text to a string
/// </summary>
public class AddTextSubWorkflow : Composite
{
    public Input<string> A { get; set; } = null!;
    public Output<string> B { get; set; } = null!;

    public AddTextSubWorkflow()
    {
        var setOutput = new SetOutput()
        {
            OutputName = new("B"),
            OutputValue = new(context => "hi there " + A.Get(context))
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