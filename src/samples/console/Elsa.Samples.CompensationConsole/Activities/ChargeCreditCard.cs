using Elsa.ActivityResults;
using Elsa.Services;

namespace Elsa.Samples.CompensationConsole.Activities;

public class ChargeCreditCard : Activity
{
    protected override IActivityExecutionResult OnExecute()
    {
        Console.WriteLine("Charging credit card for flight");
        return Done();
    }
}