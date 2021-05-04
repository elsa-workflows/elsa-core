using System;
using Elsa.Activities.Console;
using Elsa.Activities.Primitives;
using Elsa.Activities.Rebus;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using Elsa.Samples.MultiTenantChildWorker.Messages;
using NodaTime;

namespace Elsa.Samples.MultiTenantChildWorker.Workflows
{
    /// <summary>
    /// Generate a new order for a random customer every 5 seconds.  
    /// </summary>
    public class GenerateOrdersWorkflow : IWorkflow
    {
        private readonly Random _random;
        public GenerateOrdersWorkflow() => _random = new Random();

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .Timer(Duration.FromSeconds(5))
                .SetVariable("CustomerId", SelectRandomCustomerId)
                .WriteLine(context => $"Creating a new order for customer {context.GetVariable<string>("CustomerId")}.")
                .Then<SendRebusMessage>(message => message.Set(x => x.Message, context => new OrderReceived { CustomerId = context.GetVariable<string>("CustomerId") }));
        }

        private string SelectRandomCustomerId()
        {
            var customers = new[] { "Customer1", "Customer2" };
            var index = _random.Next(customers.Length);
            return customers[index];
        }
    }
}