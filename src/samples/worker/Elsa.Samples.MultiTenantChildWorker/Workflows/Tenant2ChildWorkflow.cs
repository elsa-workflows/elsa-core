using Elsa.Activities.Console;
using Elsa.Builders;

namespace Elsa.Samples.MultiTenantChildWorker.Workflows
{
    public class Tenant2ChildWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WithWorkflowDefinitionId("ProcessOrderWorkflow")
                .WithTenantId("Customer2")
                .WriteLine("Specialized workflow for Customer 2");
        }
    }
}