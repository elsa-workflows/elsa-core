using Elsa.Activities.Mqtt.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Mqtt;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt.Services
{
    public class MqttClientWrapper : IMqttClientWrapper
    {
        private readonly ILogger _logger;
        private Func<MqttApplicationMessage, Task>? _messageHandler;

        public IMqttClient Client { get; }
        public MqttClientOptions Options { get; }


        public MqttClientWrapper(IMqttClient client, MqttClientOptions options, ILogger<MqttClientWrapper> logger)
        {
            Client = client;
            Options = options;
            _logger = logger;
        }

        public async Task SubscribeAsync(string topic)
        {
            await ConnectAsync();

            await Client.SubscribeAsync(topic, Options.QualityOfService);

            Client.MessageStream.Subscribe(async message =>
            {
                if (_messageHandler != null)
                    await _messageHandler(message);
                else
                    _logger.LogWarning("Attempted to subscribe to topic {Topic}, but no message handler was set.", Options.Topic);

                await Client.UnsubscribeAsync(topic);
                await DisconnectAsync();
            });
        }

        public async Task PublishMessageAsync(string topic, string message)
        {
            await ConnectAsync();

            var applicationMessage = CreateApplicationMessage(topic, message);

            await Client.PublishAsync(applicationMessage, Options.QualityOfService);

            await DisconnectAsync();
        }

        public void SetMessageHandler(Func<MqttApplicationMessage, Task> handler)
        {
            _messageHandler = handler;
        }

        public void Dispose()
        {
            if (Client.IsConnected) DisconnectAsync().Wait();
            Client.Dispose();
        }

        private async Task ConnectAsync()
        {
            if (!Client.IsConnected) await Client.ConnectAsync(Options.GenerateMqttClientCredentials(), null, true); 
        }

        private async Task DisconnectAsync()
        {
            if (Client.IsConnected) await Client.DisconnectAsync();
        }

        private MqttApplicationMessage CreateApplicationMessage(string topic, string message) => new(topic, Encoding.UTF8.GetBytes(message));
    }
}
