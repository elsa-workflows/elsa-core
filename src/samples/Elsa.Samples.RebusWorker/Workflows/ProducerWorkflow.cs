using System;
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
            var greetings = new[]
            {
                new Greeting
                {
                    From = "John",
                    To = "Jill",
                    Message = "Hello!"
                },
                new Greeting
                {
                    From = "Julia",
                    To = "Miriam",
                    Message = "Happy Monday!"
                },
                new Greeting
                {
                    From = "Jack",
                    To = "Bob",
                    Message = "How do you do?"
                }
            };

            var index = _random.Next(0, greetings.Length);
            return greetings[index];
        }
    }
}