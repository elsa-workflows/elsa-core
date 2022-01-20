using Elsa.Activities.Console;
using Elsa.Activities.MassTransit;
using Elsa.Builders;
using Elsa.Samples.MassTransitRabbitMq.Messages;

namespace Elsa.Samples.MassTransitRabbitMq.Workflows
{
    public class InterfaceTestWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .ReceiveMassTransitMessage(
                    activity => activity.Set(x => x.MessageType, x => typeof(IInterfaceMessage))
                )
                .WriteLine(context => $"Received interface message");
        }
    }
}
