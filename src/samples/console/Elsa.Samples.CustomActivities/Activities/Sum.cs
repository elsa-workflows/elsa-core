using Elsa.Extensions;
using Elsa.Workflows.Core.Models;

namespace Elsa.Samples.CustomActivities.Activities;

/// <summary>
/// Sums two numbers.
/// </summary>
public class Sum : CodeActivity<int>
{
    public Sum(Variable<int> a, Variable<int> b, Variable<int> result)
    {
        A = new(a);
        B = new(b);
        Result = new(result);
    }

    public Input<int> A { get; set; } = default!;
    public Input<int> B { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var input1 = A.Get(context);
        var input2 = B.Get(context);
        var result = input1 + input2;
        context.SetResult(result);
    }
}