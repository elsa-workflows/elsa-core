using Elsa.Activities.Console;
using Elsa.Builders;

namespace Elsa.Samples.Persistence.EntityFramework
{
    public class HelloWorld : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder.WriteLine("Hello World!");
        }
    }
}