using System;
using Elsa.Activities.AzureServiceBus.Activities;
using Elsa.Activities.Console;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Samples.AzureServiceBusWorker.Messages;
using NodaTime;

namespace Elsa.Samples.AzureServiceBusWorker.Workflows
{
    public class ProducerWorkflow : IWorkflow
    {
        private readonly IClock _clock;
        private readonly Random _random;

        public ProducerWorkflow(IClock clock)
        {
            _clock = clock;
            _random = new Random();
        }

        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .InstantEvent(_clock.GetCurrentInstant().Plus(Duration.FromSeconds(5)))
                .WriteLine("Sending a random greeting to the \"greetings\" queue.")
                .Then<SendAzureServiceBusMessage>(sendMessage => sendMessage
                    .Set(x => x.Message, GetRandomGreeting)
                    .Set(x => x.QueueName, "greetings"));
        }

        private Greeting GetRandomGreeting()
        {
            var names = new[] { "John", "Jill", "Julia", "Miriam", "Jack", "Bob" };
            var messages = new[] { "Hello!", "How do you do?", "Happy Monday!" };
            var from = _random.Next(0, names.Length);
            var to = _random.Next(0, names.Length);
            var message = _random.Next(0, messages.Length);

            return new Greeting
            {
                From = names[from],
                To = names[to],
                Message = messages[message]
            };
        }
    }
}