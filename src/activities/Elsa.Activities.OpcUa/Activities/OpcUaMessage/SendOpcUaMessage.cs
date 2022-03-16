using Elsa.Activities.OpcUa.Configuration;
using Elsa.Activities.OpcUa.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Activities.OpcUa
{
    [Trigger(
        Category = "OpcUa",
        DisplayName = "Send OpcUa message",
        Description = "Send Message to OpcUa",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SendOpcUaMessage : Activity
    {
        private readonly IMessageSenderClientFactory _messageSenderClientFactory;

        public SendOpcUaMessage(IMessageSenderClientFactory messageSenderClientFactory)
        {
            _messageSenderClientFactory = messageSenderClientFactory;
        }

        [ActivityInput(
            Hint = "OpcUa connection string [opc.tcp://localhost:50000]",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Order = 1,
            Category = PropertyCategories.Configuration)]
        public string ConnectionString { get; set; } = default!;


        [ActivityInput(
            Hint = "OperationTimeout",
            SupportedSyntaxes = new[] { SyntaxNames.Json },
            Order = 2,
            Category = PropertyCategories.Configuration)]
        public int Timeout { get; set; } = 15000;

        [ActivityInput(
            Hint = "SessionTimeout",
            SupportedSyntaxes = new[] { SyntaxNames.Json },
            Order = 2,
            Category = PropertyCategories.Configuration)]
        public int SessionTimeout { get; set; } = 60000;

        [ActivityInput(
            Hint = "List of item monitored",
            Order = 3,
            UIHint = ActivityInputUIHints.Dictionary,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json })]
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        [ActivityInput(
            Hint = "Message body",
            Order = 4,
            UIHint = ActivityInputUIHints.MultiLine,
            SupportedSyntaxes = new[] { SyntaxNames.Json })]
        public string Message { get; set; } = default!;

        public string ClientId => $"Elsa-{Id.ToUpper()}";

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var activityId = context.ActivityId;
            var clientId = $"Elsa-{activityId.ToUpper()}";
            var config = new OpcUaBusConfiguration(ConnectionString, clientId, Tags);

            var client = await _messageSenderClientFactory.GetSenderAsync(config);

            await client.PublishMessage(Message);

            return Done();
        }
    }
}
