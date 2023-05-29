using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Kafka.Configuration;

namespace Elsa.Activities.Kafka.Services
{
    public class BusClientFactory : IMessageReceiverClientFactory, IMessageSenderClientFactory
    {
        private readonly IDictionary<int, IClient> _receivers = new Dictionary<int, IClient>();
        private readonly IDictionary<int, IClient> _senders = new Dictionary<int, IClient>();

        private readonly SemaphoreSlim _semaphore = new(1);

        private readonly KafkaOptions _options;
        public BusClientFactory(KafkaOptions options)
        {
            _options = options;
        }

        public async Task<IClient> GetReceiverAsync(KafkaConfiguration configuration, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (_receivers.TryGetValue(configuration.GetHashCode(), out var messageReceiver))
                    return messageReceiver;

                var newMessageReceiver = new Client(configuration, _options);
                _receivers.Add(configuration.GetHashCode(), newMessageReceiver);
                return newMessageReceiver;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DisposeReceiverAsync(IClient receiverClient, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            var key = GetKeyFor(receiverClient);

            try
            {
                _receivers.Remove(key);
                await receiverClient.Dispose();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IClient> GetSenderAsync(KafkaConfiguration configuration, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (_senders.TryGetValue(configuration.GetHashCode(), out var messageSender))
                    return messageSender;

                var newMessageSender = new Client(configuration, _options);
                _senders.Add(configuration.GetHashCode(), newMessageSender);
                return newMessageSender;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DisposeSenderAsync(IClient senderClient, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            var key = GetKeyFor(senderClient);

            try
            {
                _senders.Remove(key);
                await senderClient.Dispose();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static int GetKeyFor(IClient client) => client.Configuration.GetHashCode();
    }
}