using Elsa.Activities.Console;
using Elsa.Activities.MassTransit;
using Elsa.Builders;
using Elsa.Samples.MassTransitRabbitMq.Messages;
using Elsa.Activities.ControlFlow;

namespace Elsa.Samples.MassTransitRabbitMq.Workflows
{
    public class TestWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .ReceiveMassTransitMessage(
                    activity => activity.Set(x => x.MessageType, x => typeof(FirstMessage))
                )
                .Correlate(activity => activity.Set(x => x.Value, context => context.GetInput<FirstMessage>().CorrelationId.ToString()))
                .WriteLine(context => $"Received first message")

                // Wait until second message received with the same correlation id
                .ReceiveMassTransitMessage(
                    activity => activity.Set(x => x.MessageType, x => typeof(SecondMessage))
                )
                .WriteLine(context => $"Received second message");
        }
    }
}