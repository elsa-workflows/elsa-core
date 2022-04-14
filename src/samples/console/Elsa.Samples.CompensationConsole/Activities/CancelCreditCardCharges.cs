using Elsa.ActivityResults;
using Elsa.Services;

namespace Elsa.Samples.CompensationConsole.Activities;

public class CancelCreditCardCharges : Activity
{
    protected override IActivityExecutionResult OnExecute()
    {
        Console.WriteLine("Cancelling credit card charges");
        return Done();
    }
}