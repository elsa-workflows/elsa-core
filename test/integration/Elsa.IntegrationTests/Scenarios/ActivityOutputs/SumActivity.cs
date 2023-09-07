using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;

namespace Elsa.IntegrationTests.Scenarios.ActivityOutputs;

public class SumActivity : CodeActivity<int>
{
    public SumActivity(Variable<int> a, Variable<int> b)
    {
        A = new(a);
        B = new(b);
    }

    public Input<int> A { get; set; }
    public Input<int> B { get; set; }

    protected override void Execute(ActivityExecutionContext context)
    {
        var input1 = A.Get(context);
        var input2 = B.Get(context);
        var result = input1 + input2;
        
        // All three work.
        Result.Set(context, result);
    }
}