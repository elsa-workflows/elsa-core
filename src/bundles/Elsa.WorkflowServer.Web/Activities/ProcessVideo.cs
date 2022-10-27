using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.WorkflowServer.Web.Activities;

/// <summary>
/// A sample activity that simulates doing some very heavy lifting.
/// </summary>
[Activity("Demo", "Demo", "Simulates very heavy lifting, which takes 15 seconds to complete.", Kind = ActivityKind.Task)]
public class ProcessVideo : Activity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        Console.WriteLine("Begin processing...");
        await Task.Delay(15000);
        Console.WriteLine("Finished processing");
    }
}