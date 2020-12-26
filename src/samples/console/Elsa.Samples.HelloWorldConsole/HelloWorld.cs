using Elsa.Activities.Console;
using Elsa.Builders;

namespace Elsa.Samples.HelloWorldConsole
{
    /// <summary>
    /// A basic workflow with just one WriteLine activity.
    /// </summary>
    public class HelloWorld : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow) => workflow
            .WriteLine("Hello World!");
    }
}