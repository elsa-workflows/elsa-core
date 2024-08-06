using Elsa.Email.Activities;
using Elsa.Extensions;
using Elsa.Samples.AspNet.WorkflowContexts.Extensions;
using Elsa.Samples.AspNet.WorkflowContexts.Providers;
using Elsa.Scheduling.Activities;
using Elsa.WorkflowContexts.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Samples.AspNet.WorkflowContexts.Workflows;

/// A workflow that sends helpful emails to customers.
public class CustomerCommunicationsWorkflow : WorkflowBase
{
    private static string CustomerId = "2";

    /// <inheritdoc />
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.AddWorkflowContextProvider<CustomerWorkflowContextProvider>();

        builder.Root = new Sequence
        {
            Activities =
            {
                new Scheduling.Activities.Timer(TimeSpan.FromSeconds(5))
                {
                    CanStartWorkflow = false
                },
                SetWorkflowContextParameter.For<CustomerWorkflowContextProvider>(CustomerId),
                Delay.FromSeconds(5),
                new SendEmail
                {
                    Subject = new(context => $"Welcome to our family, {context.GetCustomer().Name}!"),
                    Body = new("Welcome aboard!"),
                    To = new(context => [context.GetCustomer().Email])
                },
                Delay.FromSeconds(5),
                new SendEmail
                {
                    Subject = new(context => $"{context.GetCustomer().Name}, we got great deals for you!"),
                    Body = new("Get your creditcard ready!"),
                    To = new(context => [context.GetCustomer().Email])
                },
                Delay.FromSeconds(5),
                new SendEmail
                {
                    Subject = new(context => $"{context.GetCustomer().Name}, you're missing out!"),
                    Body = new("Sale ends in 2 hours!"),
                    To = new(context => [context.GetCustomer().Email])
                },
                Delay.FromSeconds(5),
                new SendEmail
                {
                    Subject = new(context => $"{context.GetCustomer().Name}, the clock is ticking!"),
                    Body = new("Tick tik tick!"),
                    To = new(context => [context.GetCustomer().Email])
                },
            }
        };
    }
}