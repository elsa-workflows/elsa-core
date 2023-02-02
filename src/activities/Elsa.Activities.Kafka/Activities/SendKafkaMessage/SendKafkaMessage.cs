using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka;
using Elsa.Activities.Kafka.Configuration;
using Elsa.Activities.Kafka.Helpers;
using Elsa.Activities.Kafka.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Kafka.Activities.SendKafkaMessage
{
    [Trigger(
        Category = "Kafka",
        DisplayName = "Send Kafka message",
        Description = "Send Kafka to RabbitMQ",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SendKafkaMessage : Activity
    {
        private readonly IMessageSenderClientFactory _messageSenderClientFactory;

        public SendKafkaMessage(IMessageSenderClientFactory messageSenderClientFactory)
        {
            _messageSenderClientFactory = messageSenderClientFactory;
        }

        [ActivityInput(
            Hint = "Topic to listen to",
            Order = 1,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Topic { get; set; } = default!;

        [ActivityInput(
            Hint = "List of headers that should be present in the message",
            Order = 3,
            UIHint = ActivityInputUIHints.Dictionary,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript })]

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        [ActivityInput(
            Hint = "Kafka ConnectionString",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Order = 1,
            Category = PropertyCategories.Configuration)]
        public string ConnectionString { get; set; } = default!;

        [ActivityInput(
            Hint = "Message body",
            Order = 4,
            UIHint = ActivityInputUIHints.MultiLine,
            SupportedSyntaxes = new[] { SyntaxNames.Json })]
        public string Message { get; set; } = default!;


        public string ClientId => KafkaClientConfigurationHelper.GetClientId(Id);

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var config = new KafkaConfiguration(ConnectionString, Topic, "", Headers, ClientId);

            var client = await _messageSenderClientFactory.GetSenderAsync(config);

            await client.PublishMessage(Message);

            return Done();
        }
    }
}