using Elsa.Attributes;
using Elsa.Models;
using Elsa.Signals;

namespace Elsa.Activities;

[Activity("Elsa", "Control Flow", "Break out of a loop")]
public class Break : Activity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await context.SignalAsync(new BreakSignal());
    }
}