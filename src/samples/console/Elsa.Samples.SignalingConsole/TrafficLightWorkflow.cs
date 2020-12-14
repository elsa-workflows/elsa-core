using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Samples.SignalingConsole
{
    public class TrafficLightWorkflow : IWorkflow
    {   
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WriteLine(context => $"{GetCarName(context)} is approaching red traffic light...")
                .SignalReceived("Green")
                .WriteLine(context => $"Light turned green for {GetCarName(context)}. Hit that power pedal!");
        }

        private string GetCarName(ActivityExecutionContext context) => context.WorkflowExecutionContext.CorrelationId;
    }
}