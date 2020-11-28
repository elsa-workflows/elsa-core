using Elsa.Activities.Console;
using Elsa.Builders;

namespace Elsa.Samples.CustomAttributesChildWorker.Workflows
{
    public class Customer1Workflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WithCustomAttribute("Customer","Customer1")
                .WriteLine("Specialized workflow for Customer 1");
        }
    }
}