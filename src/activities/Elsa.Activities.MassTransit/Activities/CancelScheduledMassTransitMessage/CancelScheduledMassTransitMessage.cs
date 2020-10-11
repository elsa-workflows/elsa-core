using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.MassTransit.Options;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using MassTransit;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    [ActivityDefinition(
        Category = "MassTransit",
        DisplayName = "Cancel scheduled MassTransit Message",
        Description = "Cancel a scheduled message via MassTransit."
    )]
    public class CancelScheduledMassTransitMessage : MassTransitBusActivity
    {
        private readonly MessageScheduleOptions _options;

        public CancelScheduledMassTransitMessage(IBus bus, ConsumeContext consumeContext, IOptions<MessageScheduleOptions> options)
            : base(bus, consumeContext)
        {
            _options = options.Value;
        }

        [ActivityProperty(Hint = "Expression that returns the tokenId of a scheduled message to cancel.")]
        public string? TokenId { get; set; }

        protected override bool OnCanExecute(ActivityExecutionContext context) => TokenId != null && _options.SchedulerAddress != null;

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var endpoint = await SendEndpointProvider.GetSendEndpoint(_options.SchedulerAddress);
            await endpoint.CancelScheduledRecurringSend(TokenId!, "");
            return Done();
        }
    }
}