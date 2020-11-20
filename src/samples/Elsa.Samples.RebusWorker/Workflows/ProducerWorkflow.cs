using System;
using System.Linq;
using Elsa.Activities.Console;
using Elsa.Activities.Rebus;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Samples.RebusWorker.Messages;
using NodaTime;

namespace Elsa.Samples.RebusWorker.Workflows
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
                .TimerEvent(Duration.FromSeconds(5))
                .WriteLine("Sending a random greeting to the \"greetings\" queue.")
                .Then<PublishMessage>(sendMessage => sendMessage.Set(x => x.Message, GetRandomGreeting))
                //.Then<SendMessage>(sendMessage => sendMessage.Set(x => x.Message, GetRandomGreeting))
                .WriteLine(() => $"Message sent at {_clock.GetCurrentInstant()}");
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