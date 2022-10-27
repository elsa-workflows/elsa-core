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
        const int frameCount = 15;
        Console.WriteLine("Begin processing...");
        for(var frame = 0; frame < frameCount; frame++)
        {
            await Task.Delay(500);
            Console.WriteLine("Processing frame {0}", frame + 1);
        }
        Console.WriteLine("Finished processing");
    }
}