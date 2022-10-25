using System.Threading.Tasks;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Activities;

[Activity("Elsa", "Control Flow", "Break out of a loop")]
public class Break : Activity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await context.SignalAsync(new BreakSignal());
    }
}