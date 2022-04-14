using Elsa.ActivityResults;
using Elsa.Services;

namespace Elsa.Samples.CompensationConsole.Activities;

public class CancelFlight : Activity
{
    protected override IActivityExecutionResult OnExecute()
    {
        Console.WriteLine("Cancelling flight");
        return Done();
    }
}