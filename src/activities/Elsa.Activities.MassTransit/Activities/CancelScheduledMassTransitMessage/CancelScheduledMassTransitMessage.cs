using System.Threading.Tasks;
using Elsa.Activities.MassTransit.Options;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services.Models;
using MassTransit;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    [Action(
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

        [ActivityInput(Hint = "Expression that returns the tokenId of a scheduled message to cancel.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? TokenId { get; set; }

        protected override bool OnCanExecute(ActivityExecutionContext context) => TokenId != null && _options.SchedulerAddress != null;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext activityExecutionContext)
        {
            var endpoint = await SendEndpointProvider.GetSendEndpoint(_options.SchedulerAddress);
            await endpoint.CancelScheduledRecurringSend(TokenId!, "");
            return Done();
        }
    }
}