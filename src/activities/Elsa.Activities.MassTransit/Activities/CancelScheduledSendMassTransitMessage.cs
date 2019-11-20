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
        DisplayName = "Cancel scheduled MassTransit Message",
        Description = "Cancel a scheduled message via MassTransit."
    )]
    public class CancelScheduledMassTransitMessage : Activity
    {
        private readonly ISendEndpointProvider sender;
        private readonly IWorkflowExpressionEvaluator evaluator;

        public CancelScheduledMassTransitMessage(ISendEndpointProvider sender, IWorkflowExpressionEvaluator evaluator)
        {
            this.sender = sender;
            this.evaluator = evaluator;
        }

        [ActivityProperty(Hint = "Expression that returns the tokenId of a scheduled message to cancel.")]
        public WorkflowExpression TokenId
        {
            get => GetState<WorkflowExpression>();
            set => SetState(value);
        }


        protected override bool OnCanExecute(WorkflowExecutionContext context)
        {
            return TokenId != null;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            var tokenId = (Guid)await evaluator.EvaluateAsync(TokenId, typeof(Guid), context, cancellationToken);

            var endpoint = await sender.GetSendEndpoint(new Uri("rabbitmq://localhost/sample_quartz_scheduler"));

            await endpoint.CancelScheduledSend(tokenId);

            return Done();
        }
    }
}