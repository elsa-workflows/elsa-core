using Elsa.Expressions.Models;
using Elsa.Scheduling.Activities;
using Elsa.Workflows;

namespace Elsa.Server.Web.Activities;

public class CustomTimer : TimerBase
{
    protected override TimeSpan GetInterval(ExpressionExecutionContext context)
    {
        return TimeSpan.FromSeconds(5);
    }

    protected override void OnTimerElapsed(ActivityExecutionContext context)
    {
        Console.WriteLine("Timer elapsed");
    }
}