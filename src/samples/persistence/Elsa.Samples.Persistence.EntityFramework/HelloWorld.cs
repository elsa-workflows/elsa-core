using Elsa.Activities.Console;
using Elsa.Builders;

namespace Elsa.Samples.Persistence.EntityFramework
{
    public class HelloWorld : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow.WriteLine("Hello World!");
        }
    }
}