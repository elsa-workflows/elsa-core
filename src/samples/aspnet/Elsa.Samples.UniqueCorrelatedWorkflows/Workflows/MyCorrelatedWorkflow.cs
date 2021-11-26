using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Samples.UniqueCorrelatedWorkflows.Workflows
{
    public class MyCorrelatedWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine("I started! Send me the \"Continue\" signal to continue.")
                .SignalReceived("Continue")
                .WriteLine("Thank you. Now I can complete my work.")
                .WriteLine("Good bye!")
                ;
        }
    }
}