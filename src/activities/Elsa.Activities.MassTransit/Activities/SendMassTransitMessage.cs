using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services.Models;
using MassTransit;

namespace Elsa.Activities.MassTransit.Activities
{
    [ActivityDefinition(
        Category = "MassTransit",
        DisplayName = "Send MassTransit Message",
        Description = "Send a message via MassTransit."
    )]
    public class SendMassTransitMessage : MassTransitBusActivity
    {
        public SendMassTransitMessage(ConsumeContext consumeContext, IBus bus) : base(bus, consumeContext)
        {
        }

        [ActivityProperty(Hint = "An expression that evaluates to the message to send.")]
        public IWorkflowExpression Message
        {
            get => GetState<IWorkflowExpression>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The address of a specific endpoint to send the message to.")]
        public Uri EndpointAddress
        {
            get
            {
                var endpointAddress = GetState<string>();
                return string.IsNullOrEmpty(endpointAddress) ? null : new Uri(endpointAddress);
            }
            set => SetState(value.ToString());
        }

        protected override bool OnCanExecute(ActivityExecutionContext context)
        {
            return Message != null;
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var message = await context.EvaluateAsync(Message, cancellationToken);

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