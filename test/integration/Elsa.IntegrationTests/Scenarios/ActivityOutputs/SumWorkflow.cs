using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;

namespace Elsa.IntegrationTests.Scenarios.ActivityOutputs;

public class SumWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var a = new Variable<int>();
        var b = new Variable<int>();

        var sumActivity = new SumActivity(a, b);

        workflow.Root = new Sequence
        {
            Variables = { a, b },
            Activities =
            {
                new Inline(context => a.Set(context, 4)),
                new Inline(context => b.Set(context, 6)),
                sumActivity,
                new WriteLine(context => $"The result of {a.Get(context)} and {b.Get(context)} is {sumActivity.GetResult<int>(context)}."),
                new WriteLine(context => $"The last result is {context.GetLastResult<int>()}."),
            }
        };
    }
}