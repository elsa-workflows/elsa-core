using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus;
using Elsa.Activities.AzureServiceBus.ActivityExecutionResults;
using NodaTime;

namespace Elsa.Activities.AzureServiceBus
{
    public abstract class AzureServiceBusSendActivity : Activity
    {
        private readonly IContentSerializer _serializer;
        protected AzureServiceBusSendActivity(IContentSerializer serializer) => _serializer = serializer;

        [ActivityInput(SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Json })]
        public object Message { get; set; } = default!;

        [ActivityInput(Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? CorrelationId { get; set; }

        [ActivityInput(Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string ContentType { get; set; } = default!;

        [ActivityInput(Label = "Subject", Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? Label { get; set; }

        [ActivityInput(Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? To { get; set; }

        [ActivityInput(Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? MessageId { get; set; }

        [ActivityInput(Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? PartitionKey { get; set; }

        [ActivityInput(Label = "Transaction Partition Key", Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? ViaPartitionKey { get; set; }

        [ActivityInput(Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? ReplyTo { get; set; }

        [ActivityInput(Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? SessionId { get; set; }

        [Obsolete("Just use TimeToLive")]
        [ActivityInput(Category = PropertyCategories.Obsolete, Hint = "Use Time to Live instead Expires At Utc", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public Instant ExpiresAtUtc { get; set; }

        [ActivityInput(Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public Duration? TimeToLive { get; set; }

        [ActivityInput(Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? ReplyToSessionId { get; set; }

        [ActivityInput(Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public Instant? ScheduledEnqueueTimeUtc { get; set; }

        [ActivityInput(Category = PropertyCategories.Advanced, DefaultSyntax = SyntaxNames.Json, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Json }, UIHint = ActivityInputUIHints.MultiLine)]
        public IDictionary<string, object>? UserProperties { get; set; } = new Dictionary<string, object>();

        [ActivityInput(Category = PropertyCategories.Advanced, Hint = "If set, the message will be sent to the Service Bus only after the workflow is suspended, which is useful for the Request/Response pattern.", DefaultValue = false)]
        public bool SendMessageOnSuspend { get; set; }

        protected virtual string? GetQueue() => default;
        protected virtual string? GetTopic() => default;

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var message = CreateMessage(Message);

            if (!string.IsNullOrWhiteSpace(context.WorkflowExecutionContext.CorrelationId))
                message.CorrelationId = context.WorkflowExecutionContext.CorrelationId;

            return Combine(Done(), new ServiceBusActionResult(GetQueue(), GetTopic(), message, SendMessageOnSuspend));
        }

        protected ServiceBusMessage CreateMessage(Object input)
        {
            var message = Extensions.MessageBodyExtensions.CreateMessage(_serializer, input);

            message.CorrelationId = CorrelationId;
            message.ContentType = ContentType;
            message.Subject = Label;
            message.TransactionPartitionKey = ViaPartitionKey;
            message.To = To;
            message.PartitionKey = PartitionKey;
            message.ReplyTo = ReplyTo;
            message.SessionId = SessionId;
            message.ReplyToSessionId = ReplyToSessionId;

            if (MessageId != null)
                message.MessageId = MessageId;

            if (TimeToLive != null && TimeToLive > Duration.Zero)
                message.TimeToLive = TimeToLive.Value.ToTimeSpan();

            if (ScheduledEnqueueTimeUtc != null)
                message.ScheduledEnqueueTime = ScheduledEnqueueTimeUtc.Value.ToDateTimeUtc();

            if (UserProperties != null)
                foreach (var props in UserProperties)
                    message.ApplicationProperties.Add(props.Key, props.Value);

            return message;
        }
    }
}