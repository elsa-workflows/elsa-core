using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Elsa.Activities.Kafka.Configuration;
using Elsa.Activities.Kafka.Helpers;
using Elsa.Activities.Kafka.Models;
using Headers = Confluent.Kafka.Headers;

namespace Elsa.Activities.Kafka.Services
{
    public class Client : IClient
    {
        private CancellationToken _cancellationToken;
        private IConsumer<Ignore, string>? _consumer;
        private Func<KafkaMessageEvent, Task>? _messageHandler;
        private Func<Exception, Task>? _errHandler;

        public Client(KafkaConfiguration configuration)
        {
            Configuration = configuration;
        }

        public KafkaConfiguration Configuration { get; }

        public void SetHandlers(Func<KafkaMessageEvent, Task> receiveHandler, Func<Exception, Task> errorHandler, CancellationToken cancellationToken)
        {
            _messageHandler = receiveHandler;
            _cancellationToken = cancellationToken;
            _errHandler = errorHandler;
        }

        public Task StartProcessing(string topic, string group)
        {

            var consumerConfig = new ConsumerConfig(Configuration.Headers)
            {
                BootstrapServers = Configuration.ConnectionString,
                GroupId = group,
                AutoOffsetReset = Configuration.AutoOffsetReset,
            };

            _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();

            if (_consumer != null)
                Consumer.Consume(topic, _consumer).Subscribe(HandleMessage, OnError, _cancellationToken);

            return Task.CompletedTask;
        }

        public async Task PublishMessage(string message)
        {
            var producerConfig = new ProducerConfig()
            {
                BootstrapServers = Configuration.ConnectionString
            };

            using var producer = new ProducerBuilder<Null, string>(producerConfig).Build();
            await producer.ProduceAsync(Configuration.Topic, new Message<Null, string> { Headers = GetHeaders(), Value = message }, _cancellationToken);
        }

        public async Task Dispose()
        {
            if (_consumer != null)
            {
                try
                {
                    _consumer.Unsubscribe();
                    _consumer.Close();
                }
                catch (Exception e)
                {
                    if (_errHandler != null) await _errHandler(e);
                }
            }
        }

        private async void OnError(Exception error)
        {
            if (_errHandler != null) await _errHandler(error);
        }

        private async void HandleMessage(Message<Ignore, string> message)
        {
            var ev = new KafkaMessageEvent(message, _cancellationToken);

            if (_messageHandler != null)
                await _messageHandler(ev);
        }

        private Headers GetHeaders()
        {
            var headers = new Headers();

            foreach (var entry in Configuration.Headers)
                headers.Add(entry.Key, Encoding.ASCII.GetBytes(entry.Value));

            return headers;
        }
    }
}