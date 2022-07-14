using Elsa.Activities.Compensation;
using Elsa.Activities.Primitives;
using Elsa.Builders;
using Elsa.Samples.CompensationConsole.Activities;

namespace Elsa.Samples.CompensationConsole.Workflows;

/// <summary>
/// A simple workflow that compensates for errors occuring during workflow execution.
/// Inspiration taken from https://docs.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/dd489432(v=vs.100).
/// </summary>
public class CompensableWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder builder)
    {
        builder
            .StartWith<Compensable>(compensable =>
            {
                compensable.When(OutcomeNames.Body)
                    .Then<ChargeCreditCard>()
                    .Then<ReserveFlight>();
                
                compensable.When(OutcomeNames.Compensate)
                    .Then<CancelFlight>()
                    .Then<CancelCreditCardCharges>();
                
                compensable.When(OutcomeNames.Confirm)
                    .Then<ConfirmFlight>();
                
            }).WithName("Compensable1")
            .Then<ManagerApproval>()
            .Then<Fault>(a => a.WithMessage("Critical system error!"))
            .Then<PurchaseFlight>()
            .Then<TakeFlight>()
            .Then<Confirm>(a => a.WithCompensableActivityName("Compensable1"));
    }
}