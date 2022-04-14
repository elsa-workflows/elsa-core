using Elsa.Activities.Compensation.Compensable;
using Elsa.Builders;
using Elsa.Samples.CompensationConsole.Activities;

namespace Elsa.Samples.CompensationConsole.Workflows;

/// <summary>
/// A simple workflow that compensates for errors occuring during workflow execution. 
/// </summary>
public class CompensableWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder builder)
    {
        builder
            .StartWith<Compensable>(compensable =>
            {
                compensable.When(OutcomeNames.Body).Then<ReserveFlight>();
                compensable.When(OutcomeNames.Compensate).Then<CancelFlight>();
            })
            .Then(() => throw new Exception("Catastrophic failure!"))
            .Then<ManagerApproval>()
            .Then<PurchaseFlight>();
    }
}