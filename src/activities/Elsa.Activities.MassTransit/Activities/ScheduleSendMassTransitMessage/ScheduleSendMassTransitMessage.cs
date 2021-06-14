using System;
using System.Threading.Tasks;
using Elsa.Activities.MassTransit.Activities.ScheduleSendMassTransitMessage;
using Elsa.Activities.MassTransit.Options;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services.Models;
using MassTransit;
using Microsoft.Extensions.Options;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    [Action(
        Category = "MassTransit",
        DisplayName = "Schedule MassTransit Message",
        Description = "Schedule a message via MassTransit."
    )]
    public class ScheduleSendMassTransitMessage : MassTransitBusActivity
    {
        private readonly MessageScheduleOptions _options;

        public ScheduleSendMassTransitMessage(
            IBus bus,
            ConsumeContext consumeContext,
            IOptions<MessageScheduleOptions> options)
            : base(bus, consumeContext)
        {
            _options = options.Value;
        }

        [ActivityInput(Hint = "An expression that evaluates to the message to be delivered.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public object? Message { get; set; }

        [ActivityInput(Hint = "The address of a specific endpoint to deliver the message to.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public Uri EndpointAddress { get; set; } = default!;

        [ActivityInput(Hint = "An expression that evaluates to the date and time to deliver the message.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public Instant ScheduledTime { get; set; }

        [ActivityOutput] public object? Output { get; set; }

        protected override bool OnCanExecute(ActivityExecutionContext context) => Message != null && _options.SchedulerAddress != null;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var endpoint = await SendEndpointProvider.GetSendEndpoint(_options.SchedulerAddress);

            var scheduledMessage = await endpoint.ScheduleRecurringSend(
                EndpointAddress,
                new InstantRecurringSchedule(ScheduledTime),
                Message,
                context.CancellationToken);

            Output = scheduledMessage;
            return Done();
        }
    }
}