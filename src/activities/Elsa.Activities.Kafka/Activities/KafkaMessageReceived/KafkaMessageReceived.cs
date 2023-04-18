using System.Collections.Generic;
using Confluent.Kafka;
using Elsa.Activities.Kafka.Helpers;
using Elsa.Activities.Kafka.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Kafka.Activities.KafkaMessageReceived
{
    [Trigger(
        Category = "Kafka",
        DisplayName = "Kafka Message Received",
        Description = "Triggers when Kafka message matching",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class KafkaMessageReceived : Activity
    {
        [ActivityInput(
            Hint = "Topic to listen to",
            Order = 1,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Topic { get; set; } = default!;

        [ActivityInput(
            Hint = "Group",
            Order = 2,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Group { get; set; } = default!;

        [ActivityInput(
            Order = 3,
            DefaultValue = false,            
            SupportedSyntaxes = new[] { SyntaxNames.Literal })]
        public bool IgnoreHeaders { get; set; } = default!;

        [ActivityInput(
            Hint = "List of headers that should be present in the message",
            Order = 4,
            UIHint = ActivityInputUIHints.Dictionary,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript })]

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        [ActivityInput(
            Hint = "Kafka ConnectionString",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Order = 2,
            Category = PropertyCategories.Configuration)]
        public string ConnectionString { get; set; } = default!;

        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            OptionsProvider = typeof(AutoOffsetResetOptionsProvider),
            Category = PropertyCategories.Configuration,
            Order = 3
        )]
        public string AutoOffsetReset { get; set; } = default!;

        public string ClientId => KafkaClientConfigurationHelper.GetClientId(Id);

        [ActivityInput(
        Hint = "Schema",
        SupportedSyntaxes = new[] { SyntaxNames.Literal },
        Order = 4,
        Category = PropertyCategories.Configuration)]
        public string Schema { get; set; } = default!;

        [ActivityOutput(Hint = "Received message")]
        public object? Output { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternalAsync(context) : Suspend();

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternalAsync(context);

        private IActivityExecutionResult ExecuteInternalAsync(ActivityExecutionContext context)
        {
            var message = (MessageReceivedInput)context.Input!;

            Output = message.MessageString;

            context.LogOutputProperty(this, nameof(Output), Output);
            context.JournalData.Add("Headers", message);

            return Done();
        }
    }
}