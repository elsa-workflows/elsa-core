using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Options;
using Elsa.Activities.Mqtt.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services.Models;

namespace Elsa.Activities.Mqtt.Activities.SendMqttMessage
{
    [Trigger(
        Category = "MQTT",
        DisplayName = "Send MQTT Message",
        Description = "Sends MQTT message matching with topic",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SendMqttMessage : MqttBaseActivity
    {
        private readonly IMessageSenderClientFactory _messageSenderClientFactory;
        
        public SendMqttMessage(IMessageSenderClientFactory messageSenderClientFactory)
        {
            _messageSenderClientFactory = messageSenderClientFactory;
        }

        [ActivityInput(
            Hint = "Message body",
            Order = 2,
            UIHint = ActivityInputUIHints.MultiLine,
            SupportedSyntaxes = new[] { SyntaxNames.Json })]
        public string Message { get; set; } = default!;

        [ActivityOutput(Hint = "Received message")]
        public object? Output { get; set; }
        
        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var options = new MqttClientOptions(Topic, Host, Port, Username, Password, QualityOfService);

            var client = await _messageSenderClientFactory.GetSenderAsync(options);

            await client.PublishMessageAsync(Topic, Message); 

            return Done();
        }
    }
}