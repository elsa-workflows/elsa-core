using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.Expressions;
using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using MassTransit;

namespace Sample08.Activities
{
    public class SendMassTransitMessage<T> : Activity
    {
        private readonly ISendEndpointProvider sender;
        private readonly IWorkflowExpressionEvaluator evaluator;

        public SendMassTransitMessage(ISendEndpointProvider sender, IWorkflowExpressionEvaluator evaluator)
        {
            this.sender = sender;
            this.evaluator = evaluator;
        }
        
        public WorkflowExpression<T> Message
        {
            get => GetState(() => new WorkflowExpression<T>(JavaScriptEvaluator.SyntaxName, string.Empty));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var message = evaluator.EvaluateAsync(Message, context, cancellationToken);
            await sender.Send(message, cancellationToken);

            return Done();
        }
    }
}