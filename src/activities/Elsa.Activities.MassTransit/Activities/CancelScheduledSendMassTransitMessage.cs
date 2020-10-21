using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.MassTransit.Options;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.MassTransit.Activities
{
    [ActivityDefinition(
        Category = "MassTransit",
        DisplayName = "Cancel scheduled MassTransit Message",
        Description = "Cancel a scheduled message via MassTransit."
    )]
    public class CancelScheduledMassTransitMessage : MassTransitBusActivity
    {
        private readonly IWorkflowExpressionEvaluator evaluator;
        private readonly MessageScheduleOptions options;

        public CancelScheduledMassTransitMessage(IWorkflowExpressionEvaluator evaluator, IBus bus, ConsumeContext consumeContext, IOptions<MessageScheduleOptions> options)
            : base(bus, consumeContext)
        {
            this.evaluator = evaluator;
            this.options = options.Value;
        }

        [ActivityProperty(Hint = "Expression that returns the tokenId of a scheduled message to cancel.")]
        public WorkflowExpression TokenId
        {
            get => GetState<WorkflowExpression>();
            set => SetState(value);
        }

        protected override bool OnCanExecute(WorkflowExecutionContext context)
        {
            return TokenId != null && options.SchedulerAddress != null;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            var tokenId = (Guid)await evaluator.EvaluateAsync(TokenId, typeof(Guid), context, cancellationToken);

            var endpoint = await SendEndpointProvider.GetSendEndpoint(options.SchedulerAddress);

            await endpoint.CancelScheduledSend(tokenId);

            return Done();
        }
    }
}