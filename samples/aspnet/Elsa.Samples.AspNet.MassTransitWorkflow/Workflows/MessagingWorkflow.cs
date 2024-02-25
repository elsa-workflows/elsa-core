using Elsa.MassTransit.Activities;
using Elsa.Samples.AspNet.MassTransitWorkflow.Messages;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;

namespace Elsa.Samples.AspNet.MassTransitWorkflow.Workflows;

public class MessagingWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var busMessage = builder.WithVariable<Message>();

        builder.Root = new Sequence
        {
            Activities =
            {
                new MessageReceived
                {
                    CanStartWorkflow = true,
                    Type = ActivityTypeNameHelper.GenerateTypeName(typeof(Message)),
                    MessageType = typeof(Message),
                    Result = new (busMessage)
                },
                new WriteLine(context => $"Received message from Bus: {busMessage.Get(context)?.Content}")
            }
        };
    }
}