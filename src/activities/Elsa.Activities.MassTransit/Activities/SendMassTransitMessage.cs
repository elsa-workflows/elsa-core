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
        private readonly ISendEndpointProvider sender;
        private readonly IWorkflowExpressionEvaluator evaluator;

        public SendMassTransitMessage(ISendEndpointProvider sender, IWorkflowExpressionEvaluator evaluator)
        {
            this.sender = sender;
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

        protected override bool OnCanExecute(WorkflowExecutionContext context)
        {
            return MessageType != null;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            var message = await evaluator.EvaluateAsync(Message, MessageType, context, cancellationToken);
            await sender.Send(message, cancellationToken);

            return Done();
        }
    }
}