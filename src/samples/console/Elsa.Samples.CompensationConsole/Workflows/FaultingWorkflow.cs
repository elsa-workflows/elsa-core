using Elsa.Activities.Primitives;
using Elsa.Builders;
using Elsa.Samples.CompensationConsole.Activities;

namespace Elsa.Samples.CompensationConsole.Workflows;

/// <summary>
/// A simple workflow that uses the Fault activity to fault the workflow. 
/// </summary>
public class FaultingWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder builder)
    {
        builder
            .Then<ChargeCreditCard>()
            .Then<ReserveFlight>()
            .Then<Fault>(a => a.WithMessage("System error!"))
            .Then<ManagerApproval>()
            .Then<PurchaseFlight>();
    }
}