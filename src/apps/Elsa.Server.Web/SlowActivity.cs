using Elsa.Workflows;
using Elsa.Workflows.Attributes;

namespace Elsa.Server.Web;

[Activity(Kind = ActivityKind.Task)]
public class SlowActivity : CodeActivity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        Console.WriteLine("Starting...");
        await Task.Delay(TimeSpan.FromSeconds(45));
        Console.WriteLine("Done.");
    }
}