using Elsa.ActivityResults;
using Elsa.Services;

namespace Elsa.Samples.CompensationConsole.Activities;

public class ManagerApproval : Activity
{
    protected override IActivityExecutionResult OnExecute()
    {
        Console.WriteLine("Manager approval received");
        return Done();
    }
}