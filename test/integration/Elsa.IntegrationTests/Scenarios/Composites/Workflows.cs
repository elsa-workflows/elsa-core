using Elsa.Extensions;
using Elsa.JavaScript.Activities;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;

namespace Elsa.IntegrationTests.Scenarios.Composites;

/// <summary>
/// A composite activity that sums two inputs and returns the result.
/// </summary>
public class Sum : Composite<int>
{
    private readonly RunJavaScript _runJavaScript;

    public Input<int> A { get; set; } = default!;
    public Input<int> B { get; set; } = default!;

    public Sum()
    {
        _runJavaScript = new RunJavaScript
        {
            Script = new("getA() + getB();"),
        };

        Root = new Sequence
        {
            Activities =
            {
                _runJavaScript
            }
        };
    }

    protected override void ConfigureActivities(ActivityExecutionContext context)
    {
        // Copy input into variables so that JS activity can access them.
        var a = A.Get(context);
        var b = B.Get(context);
        Variables.Add(new Variable<int>("A", a));
        Variables.Add(new Variable<int>("B", b));

        // If the call site set a result variable, assign it to the JavaScript activity's result.
        if (Result != null)
            _runJavaScript.Result = new Output<object>(Result.MemoryBlockReference)!;
    }
}

/// <summary>
/// A workflow that uses the <see cref="Sum"/> composite activity.
/// </summary>
public class SumWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var sum = new Variable<int>();

        workflow.Root = new Sequence
        {
            Variables = { sum },
            Activities =
            {
                new Sum
                {
                    A = new(1),
                    B = new(2),
                    Result = new(sum)
                },
                new WriteLine(context => $"Sum: {sum.Get(context)}")
            }
        };
    }
}