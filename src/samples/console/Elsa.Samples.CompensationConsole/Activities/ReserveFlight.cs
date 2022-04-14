using Elsa.ActivityResults;
using Elsa.Services;

namespace Elsa.Samples.CompensationConsole.Activities;

public class ReserveFlight : Activity
{
    protected override IActivityExecutionResult OnExecute()
    {
        Console.WriteLine("Reserving flight");
        return Done();
    }
}