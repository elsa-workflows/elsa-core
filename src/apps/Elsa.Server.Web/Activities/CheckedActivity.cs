using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.UIHints.CheckList;

namespace Elsa.Server.Web.Activities;

public class CheckedActivity : CodeActivity<string[]>
{
    [Input(UIHint = InputUIHints.CheckList, UIHandler = typeof(StaticCheckListOptionsProvider), Options = new[]{"A", "B", "C"})]
    public Input<string[]> CheckedItems { get; set; }

    protected override void Execute(ActivityExecutionContext context)
    {
        context.SetResult(CheckedItems.GetOrDefault(context));
    }
}