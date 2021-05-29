using System;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services.Models;
using MassTransit;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    [Action(
        Category = "MassTransit",
        DisplayName = "Send MassTransit Message",
        Description = "Send a message via MassTransit."
    )]
    public class SendMassTransitMessage : MassTransitBusActivity
    {
        public SendMassTransitMessage(ConsumeContext consumeContext, IBus bus) : base(bus, consumeContext)
        {
        }

        [ActivityInput(Hint = "An expression that evaluates to the message to send.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public object? Message { get; set; }

        [ActivityInput(Hint = "The address of a specific endpoint to send the message to.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public Uri EndpointAddress { get; set; } = default!;

        protected override bool OnCanExecute(ActivityExecutionContext context) => Message != null;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var message = Message;
            var cancellationToken = context.CancellationToken;

            if (EndpointAddress != null)
            {
                var endpoint = await SendEndpointProvider.GetSendEndpoint(EndpointAddress);
                await endpoint.Send(message, cancellationToken);
            }
            else
            {
                await SendEndpointProvider.Send(message, cancellationToken);
            }

            return Done();
        }
    }
}