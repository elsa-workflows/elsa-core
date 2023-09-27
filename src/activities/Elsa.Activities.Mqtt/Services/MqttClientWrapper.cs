using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt.Services
{
    public class MqttClientWrapper : IMqttClientWrapper
    {
        private readonly ILogger _logger;
        private Func<MqttApplicationMessage, Task>? _messageHandler;
        private readonly SemaphoreSlim _semaphore = new(1);
        public IMqttClient Client { get; }
        public Options.MqttClientOptions Options { get; }

        public MqttClientWrapper(IMqttClient client, Options.MqttClientOptions options, ILogger<MqttClientWrapper> logger)
        {
            Client = client;
            Options = options;
            _logger = logger;
        }

        
        private async Task SubscribeAsync(string topic, Func<MqttApplicationMessage, Task> handler)
        {
            if (!Client.IsConnected)
            {

                _messageHandler = handler;

                var opt = Options.GenerateMqttClientOptions();
                Client.ConnectAsync(opt).Wait(); //Sync
                await Client.SubscribeAsync(topic, Options.QualityOfService);
                Client.ApplicationMessageReceivedAsync += async e =>
                {
                    if (_messageHandler != null)
                        await _messageHandler(e.ApplicationMessage);
                    else
                        _logger.LogWarning("Attempted to subscribe to topic {Topic}, but no message handler was set.", Options.Topic);
                };
            }
        }

        public async Task PublishMessageAsync(string topic, string message)
        {
            await ConnectAsync();

            var applicationMessage = CreateApplicationMessage(topic, message, Options.QualityOfService);

            await Client.PublishAsync(applicationMessage);

            await DisconnectAsync();
        }

        public async Task SetMessageHandlerAsync(Func<MqttApplicationMessage, Task> handler)
        {
            await SubscribeAsync(Options.Topic, handler);
        }

        public void Dispose()
        {
            if (Client.IsConnected) DisconnectAsync().Wait();
            Client.Dispose();
        }

        private async Task ConnectAsync()
        {
            if (!Client.IsConnected)
            {
                var opt = Options.GenerateMqttClientOptions();
                await Client.ConnectAsync(opt);
            }
        }

        private async Task DisconnectAsync()
        {
            if (Client.IsConnected)
            {
                await Client.DisconnectAsync();
            }
        }

        private MqttApplicationMessage CreateApplicationMessage(string topic, string message, MqttQualityOfServiceLevel qos)
        {
            var msg = new MqttApplicationMessage()
            {
                Topic = topic,
                Payload = Encoding.UTF8.GetBytes(message),
                QualityOfServiceLevel = qos
            };
            return msg;
        }

    }
}
