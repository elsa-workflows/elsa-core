using Elsa.Activities.AzureServiceBus.Activities;
using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Samples.AzureServiceBusWorker.Messages;

namespace Elsa.Samples.AzureServiceBusWorker.Workflows
{
    public class ConsumerWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .StartWith<AzureServiceBusMessageReceived>(messageReceived => messageReceived.Set(x => x.QueueName, "greetings"))
                .WriteLine(context =>
                {
                    var greeting = context.GetInput<Greeting>();
                    return $"Received a greeting from {greeting.From}, saying \"{greeting.Message}\" to {greeting.To}!";
                });
        }
    }
}