using System;
using System.Net.Mqtt;
using System.Text;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Options;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Mqtt.Services
{
    public class MqttClientWrapper : IMqttClientWrapper
    {
        public IMqttClient Client { get; }
        public MqttClientOptions Options { get; }


        public MqttClientWrapper(IMqttClient client, MqttClientOptions options)
        {
            Client = client;
            Options = options;
        }

        public async Task SubscribeWithHandlerAsync(string topic, Func<MqttApplicationMessage, Task> handler)
        {
            await ConnectAsync();

            await Client.SubscribeAsync(topic, Options.QualityOfService);

            Client.MessageStream.Subscribe(async message =>
            {
                await handler(message);
            });
        }

        public async Task PublishMessageAsync(string topic, string message)
        {
            await ConnectAsync();

            var applicationMessage = CreateApplicationMessage(topic, message);

            await Client.PublishAsync(applicationMessage, Options.QualityOfService);

            await DisconnectAsync();
        }

           
            await SubscribeAsync(Options.Topic);
            
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
