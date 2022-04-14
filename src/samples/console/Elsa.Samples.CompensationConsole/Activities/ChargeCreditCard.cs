using Elsa.ActivityResults;
using Elsa.Services;

namespace Elsa.Samples.CompensationConsole.Activities;

public class ChargeCreditCard : Activity
{
    protected override IActivityExecutionResult OnExecute()
    {
        Console.WriteLine("Charge credit card for flight");
        return Done();
    }
}