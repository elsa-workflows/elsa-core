using Elsa.Workflows;

namespace Elsa.Server.Web;

public class SlowActivity : CodeActivity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        Console.WriteLine("Starting...");
        await Task.Delay(TimeSpan.FromMinutes(2));
        Console.WriteLine("Done.");
    }
}