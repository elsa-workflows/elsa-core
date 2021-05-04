using Elsa.Activities.Console;
using Elsa.Builders;

namespace Elsa.Samples.MultiTenantChildWorker.Workflows
{
    public class Tenant1ChildWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithWorkflowDefinitionId("ProcessOrderWorkflow")
                .WithTenantId("Customer1")
                .WriteLine("Specialized workflow for Customer 1");
        }
    }
}