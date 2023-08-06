using Elsa.Email.Activities;
using Elsa.Extensions;
using Elsa.Samples.AspNet.WorkflowContexts.Providers;
using Elsa.Samples.AspNet.WorkflowContexts.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.WorkflowContexts.Activities;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Samples.AspNet.WorkflowContexts.Workflows;

/// <summary>
/// A workflow that sends annoying emails to customers.
/// </summary>
public class CustomerCommunicationsWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.AddWorkflowContextProvider<CustomerWorkflowContextProvider>();

        builder.Root = new Sequence
        {
            Activities =
            {
                SetWorkflowContextParameter.For<CustomerWorkflowContextProvider>(
                    context => context.GetInput<string>("CustomerId")!),
                Delay.FromSeconds(5),
                new SendEmail
                {
                    Subject = new(context => $"Welcome to our family, {context.GetCustomer().Name}!"),
                    Body = new("Welcome aboard!"),
                    To = new(context => new[] { context.GetCustomer().Email })
                },
                Delay.FromSeconds(5),
                new SendEmail
                {
                    Subject = new(context => $"{context.GetCustomer().Name}, we got great deals for you!"),
                    Body = new("Get your creditcard ready!"),
                    To = new(context => new[] { context.GetCustomer().Email })
                },
                Delay.FromSeconds(5),
                new SendEmail
                {
                    Subject = new(context => $"{context.GetCustomer().Name}, you're missing out!"),
                    Body = new("Sale ends in 2 hours!"),
                    To = new(context => new[] { context.GetCustomer().Email })
                },
                Delay.FromSeconds(5),
                new SendEmail
                {
                    Subject = new(context => $"{context.GetCustomer().Name}, the clock is ticking!"),
                    Body = new("Tick tik tick!"),
                    To = new(context => new[] { context.GetCustomer().Email })
                },
            }
        };
    }
}