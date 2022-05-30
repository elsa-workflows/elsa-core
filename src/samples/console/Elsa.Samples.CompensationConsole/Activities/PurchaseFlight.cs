using Elsa.ActivityResults;
using Elsa.Services;

namespace Elsa.Samples.CompensationConsole.Activities;

public class PurchaseFlight : Activity
{
    protected override IActivityExecutionResult OnExecute()
    {
        Console.WriteLine("Ticket is purchased");
        return Done();
    }
}