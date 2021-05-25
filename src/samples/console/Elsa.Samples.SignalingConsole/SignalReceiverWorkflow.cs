using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Samples.SignalingConsole
{
    public class SignalReceiverWorkflow : IWorkflow
    {   
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .SignalReceived("Demo Signal")
                .WriteLine(() => $"Signal received!");
        }
    }
}