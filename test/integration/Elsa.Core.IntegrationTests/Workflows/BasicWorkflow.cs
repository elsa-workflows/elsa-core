using Elsa.Activities.Console;
using Elsa.Builders;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class BasicWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder.WriteLine("Hello xUnit!");
        }
    }
}