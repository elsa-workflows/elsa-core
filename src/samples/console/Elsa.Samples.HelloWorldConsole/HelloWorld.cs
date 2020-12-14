using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Samples.HelloWorldConsole
{
    /// <summary>
    /// A basic workflow with just one WriteLine activity.
    /// </summary>
    public class HelloWorld : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow) => workflow
            .WithTenantId("1")
            .WriteLine("Hello World!")
            .SignalReceived("Test")
            .WriteLine("Signal received");
    }
}