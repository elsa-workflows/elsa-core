using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.MassTransit.Options;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
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
        private readonly MessageScheduleOptions options;

        public CancelScheduledMassTransitMessage(IBus bus, ConsumeContext consumeContext, IOptions<MessageScheduleOptions> options)
            : base(bus, consumeContext)
        {
            this.options = options.Value;
        }

        [ActivityProperty(Hint = "Expression that returns the tokenId of a scheduled message to cancel.")]
        public IWorkflowExpression<Guid> TokenId
        {
            get => GetState<IWorkflowExpression<Guid>>();
            set => SetState(value);
        }


        protected override bool OnCanExecute(ActivityExecutionContext context) => TokenId != null && options.SchedulerAddress != null;

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var tokenId = await activityExecutionContext.EvaluateAsync(TokenId, cancellationToken);
            var endpoint = await SendEndpointProvider.GetSendEndpoint(options.SchedulerAddress);

            await endpoint.CancelScheduledSend(tokenId);

            return Done();
        }
    }
}