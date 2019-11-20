using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using MassTransit;

namespace Elsa.Activities.MassTransit.Activities
{
    [ActivityDefinition(
        Category = "MassTransit",
        DisplayName = "Send MassTransit Message",
        Description = "Send a message via MassTransit."
    )]
    public class SendMassTransitMessage : Activity
    {
        private readonly ConsumeContext consumeContext;
        private readonly IBus bus;
        private readonly IWorkflowExpressionEvaluator evaluator;

        public SendMassTransitMessage(ConsumeContext consumeContext, IBus bus, IWorkflowExpressionEvaluator evaluator)
        {
            this.consumeContext = consumeContext;
            this.bus = bus;
            this.evaluator = evaluator;
        }

        [ActivityProperty(Hint = "The assembly-qualified type name of the message to send.")]
        public Type MessageType
        {
            get
            {
                var typeName = GetState<string>();
                return string.IsNullOrWhiteSpace(typeName) ? null : System.Type.GetType(typeName);
            }
            set => SetState(value.AssemblyQualifiedName);
        }

        [ActivityProperty(Hint = "An expression that evaluates to the message to send.")]
        public WorkflowExpression Message
        {
            get => GetState<WorkflowExpression>();
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

        /// <summary>
        /// Gets the send endpoint provider to use.
        /// </summary>
        /// <remarks>
        /// Will use the current scopes consume context if one exists to maintain
        /// the conversation and correlation id.
        /// </remarks>
        private ISendEndpointProvider SendEndpointProvider =>
            consumeContext != null
                ? (ISendEndpointProvider)consumeContext
                : (ISendEndpointProvider)bus;

        protected override bool OnCanExecute(WorkflowExecutionContext context)
        {
            return MessageType != null;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            var message = await evaluator.EvaluateAsync(Message, MessageType, context, cancellationToken);

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