using System;
using System.Collections.Generic;

namespace Elsa.Activities.AzureServiceBus.Models
{
    public class MessageModel
    {
        public byte[] Body { get; set; } = default!;
        public string? CorrelationId { get; set; }
        public string ContentType { get; set; } = default!;
        public string? Label { get; set; }
        public string? To { get; set; }
        public string MessageId { get; set; } = default!;
        public string? PartitionKey { get; set; }
        public string? ViaPartitionKey { get; set; }
        public string? ReplyTo { get; set; }
        public string? SessionId { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public TimeSpan TimeToLive { get; set; }
        public string? ReplyToSessionId { get; set; }
        public DateTime ScheduledEnqueueTimeUtc { get; set; }
        public IDictionary<string, object> UserProperties { get; set; } = new Dictionary<string,object>();

        public object ExtractHeaders() =>
            new
            {
                Label,
                To,
                ContentType,
                CorrelationId,
                MessageId,
                PartitionKey,
                ReplyTo,
                UserProperties,
                ExpiresAtUtc,
                TimeToLive,
                ViaPartitionKey,
                ReplyToSessionId,
                ScheduledEnqueueTimeUtc
            };
    }
}