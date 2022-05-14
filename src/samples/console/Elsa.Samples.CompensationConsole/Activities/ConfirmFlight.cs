using Elsa.ActivityResults;
using Elsa.Services;

namespace Elsa.Samples.CompensationConsole.Activities;

public class ConfirmFlight : Activity
{
    protected override IActivityExecutionResult OnExecute()
    {
        Console.WriteLine("Confirming flight");
        return Done();
    }
}