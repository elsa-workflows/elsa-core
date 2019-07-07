using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.Expressions;
using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using MassTransit;

namespace Elsa.Activities.MassTransit.Activities
{
    public class SendMassTransitMessage : Activity
    {
        private readonly ISendEndpointProvider sender;
        private readonly IWorkflowExpressionEvaluator evaluator;

        public SendMassTransitMessage(ISendEndpointProvider sender, IWorkflowExpressionEvaluator evaluator)
        {
            this.sender = sender;
            this.evaluator = evaluator;
        }

        public Type MessageType
        {
            get
            {
                var typeName = GetState<string>();
                return string.IsNullOrWhiteSpace(typeName) ? null : Type.GetType(typeName);
            }
            set => SetState(value.AssemblyQualifiedName);
        }

        public WorkflowExpression Message
        {
            get => GetState(() => new WorkflowExpression(JavaScriptEvaluator.SyntaxName, string.Empty));
            set => SetState(value);
        }

        protected override bool OnCanExecute(WorkflowExecutionContext context)
        {
            return MessageType != null;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var message = await evaluator.EvaluateAsync(Message, MessageType, context, cancellationToken);
            await sender.Send(message, cancellationToken);

            return Done();
        }
    }
}