using Elsa.Extensions;
using Elsa.Workflows;

namespace Elsa.Server.Web.Activities;

public class Activity1 : CodeActivity<string>
{
    protected override void Execute(ActivityExecutionContext context)
    {
        var output = "My Output";
        context.SetResult(output);
    }
}