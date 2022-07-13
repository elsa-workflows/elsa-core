using Elsa.Activities.Mqtt.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Mqtt;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt.Services
{
    public class BusClientFactory : IMessageReceiverClientFactory, IMessageSenderClientFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDictionary<int, IMqttClientWrapper> _senders = new Dictionary<int, IMqttClientWrapper>();
        private readonly IDictionary<int, IMqttClientWrapper> _receivers = new Dictionary<int, IMqttClientWrapper>();
        private readonly SemaphoreSlim _semaphore = new(1);

        public BusClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<IMqttClientWrapper> GetSenderAsync(MqttClientOptions options, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (_senders.TryGetValue(options.GetHashCode(), out var messageSender))
                    return messageSender;

                var newClient = await MqttClient.CreateAsync(options.Host, options.Port);
                var newMessageSender = ActivatorUtilities.CreateInstance<MqttClientWrapper>(_serviceProvider, newClient, options);

                _senders.Add(newMessageSender.Options.GetHashCode(), newMessageSender);
                return newMessageSender;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IMqttClientWrapper> GetReceiverAsync(MqttClientOptions options, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (_receivers.TryGetValue(options.GetHashCode(), out var messageReceiver))
                    return messageReceiver;

                var newClient = await MqttClient.CreateAsync(options.Host, options.Port);
                var newMessageReceiver = ActivatorUtilities.CreateInstance<MqttClientWrapper>(_serviceProvider, newClient, options);

                _receivers.Add(options.GetHashCode(), newMessageReceiver);
                return newMessageReceiver;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DisposeReceiverAsync(IMqttClientWrapper receiverClient, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                _receivers.Remove(receiverClient.Options.GetHashCode());
                receiverClient.Dispose();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}