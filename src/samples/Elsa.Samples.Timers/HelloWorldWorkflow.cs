using Elsa.Activities.Console;
using Elsa.Builders;

namespace Elsa.Samples.Timers
{
    public class HelloWorldWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder.WriteLine("Hello World!");
        }
    }
}