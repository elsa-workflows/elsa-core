using Elsa.ActivityResults;
using Elsa.Services;

namespace Elsa.Samples.CompensationConsole.Activities;

public class TakeFlight : Activity
{
    protected override IActivityExecutionResult OnExecute()
    {
        Console.WriteLine("Flight has been taken, no compensation possible");
        return Done();
    }
}