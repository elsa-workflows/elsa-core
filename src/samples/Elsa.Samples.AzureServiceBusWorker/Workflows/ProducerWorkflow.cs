using System;
using Elsa.Activities.AzureServiceBus;
using Elsa.Activities.Console;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Samples.AzureServiceBusWorker.Messages;
using NodaTime;

namespace Elsa.Samples.AzureServiceBusWorker.Workflows
{
    public class ProducerWorkflow : IWorkflow
    {
        private readonly Random _random;

        public ProducerWorkflow()
        {
            _random = new Random();
        }

        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .TimerEvent(Duration.FromSeconds(5))
                .WriteLine("Sending a random greeting to the \"greetings\" queue.")
                .SendMessage("greetings", GetRandomGreeting);
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