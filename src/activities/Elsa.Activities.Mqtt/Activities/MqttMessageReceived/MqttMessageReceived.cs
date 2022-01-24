using System.Net.Mqtt;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Options;
using Elsa.Activities.Mqtt.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;

namespace Elsa.Activities.Mqtt.Activities.MqttMessageReceived
{
    [Trigger(
        Category = "MQTT",
        DisplayName = "MQTT Message Received",
        Description = "Triggers when MQTT message matching specified topic is received",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class MqttMessageReceived : MqttBaseActivity
    {
        private readonly IMessageReceiverClientFactory _messageReceiver;

        public MqttMessageReceived(IMessageReceiverClientFactory messageReceiver)
        {
            _messageReceiver = messageReceiver;
        }

        [ActivityOutput(Hint = "Received message")]
        public object? Output { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternalAsync(context) : await SuspendInternalAsync();
        
        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternalAsync(context);
        
        private IActivityExecutionResult ExecuteInternalAsync(ActivityExecutionContext context)
        {
            if (context.Input != null)
            {
                var message = (MqttApplicationMessage)context.Input;
                Output = System.Text.Encoding.UTF8.GetString(message.Payload);
            }

            context.LogOutputProperty(this, nameof(Output), Output);

            return Done();
        }

        private async ValueTask<IActivityExecutionResult> SuspendInternalAsync()
        {
            var options = new MqttClientOptions(Topic, Host, Port, Username, Password, QualityOfService);

            var receiver = await _messageReceiver.GetReceiverAsync(options);

            await receiver.SubscribeAsync(Topic);

            return Suspend();
        }
    }
}