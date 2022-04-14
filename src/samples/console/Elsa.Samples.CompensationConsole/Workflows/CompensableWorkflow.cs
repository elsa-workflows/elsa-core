using Elsa.Activities.Compensation.Compensable;
using Elsa.Activities.Compensation.Compensate;
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
            }).WithName("Compensable1")
            
            // Throw an exception to trigger compensation: 
            //.Then(() => throw new Exception("Catastrophic failure!"))
            
            // Or target a specific compensable activity to trigger compensation
            .Then<Compensate>(a => a
                .WithCompensableActivityName("Compensable1")
                .WithMessage("I changed my mind!"))
            
            .Then<ManagerApproval>()
            .Then<PurchaseFlight>();
    }
}