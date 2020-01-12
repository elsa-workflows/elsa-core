using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.MassTransit.Options;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services.Models;
using MassTransit;
using Microsoft.Extensions.Options;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    [ActivityDefinition(
        Category = "MassTransit",
        DisplayName = "Schedule MassTransit Message",
        Description = "Schedule a message via MassTransit."
    )]
    public class ScheduleSendMassTransitMessage : MassTransitBusActivity
    {
        private readonly MessageScheduleOptions options;

        public ScheduleSendMassTransitMessage(IBus bus, ConsumeContext consumeContext, IOptions<MessageScheduleOptions> options)
        : base(bus, consumeContext)
        {
            this.options = options.Value;
        }

        [ActivityProperty(Hint = "An expression that evaluates to the message to be delivered.")]
        public IWorkflowExpression Message
        {
            get => GetState<IWorkflowExpression>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The address of a specific endpoint to deliver the message to.")]
        public Uri EndpointAddress
        {
            get
            {
                var endpointAddress = GetState<string>();
                return string.IsNullOrEmpty(endpointAddress) ? null : new Uri(endpointAddress);
            }
            set => SetState(value.ToString());
        }

        [ActivityProperty(Hint = "An expression that evaluates to the date and time to deliver the message.")]
        public IWorkflowExpression<Instant> ScheduledTime
        {
            get => GetState<IWorkflowExpression<Instant>>();
            set => SetState(value);
        }

        protected override bool OnCanExecute(ActivityExecutionContext context)
        {
            return Message != null && options.SchedulerAddress != null;
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var message = await context.EvaluateAsync(Message, cancellationToken);
            var scheduledTime = await context.EvaluateAsync(ScheduledTime, cancellationToken);
            var endpoint = await SendEndpointProvider.GetSendEndpoint(options.SchedulerAddress);
            var scheduledMessage = await endpoint.ScheduleSend(EndpointAddress, scheduledTime.ToDateTimeUtc(), message, cancellationToken);

            return Done(Variable.From(scheduledMessage.TokenId));
        }
    }
}