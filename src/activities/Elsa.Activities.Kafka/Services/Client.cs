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
        private IConsumer<Ignore, byte[]>? _consumer;
        private Func<KafkaMessageEvent, Task>? _messageHandler;
        private Func<Exception, Task>? _errHandler;
        private readonly KafkaOptions _kafkaOptions;

        public Client(KafkaConfiguration configuration, KafkaOptions options)
        {
            Configuration = configuration;
            _kafkaOptions = options;
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
                SaslMechanism = _kafkaOptions.SaslMechanism,
                SaslUsername = _kafkaOptions.SaslUsername,
                SecurityProtocol = _kafkaOptions.SecurityProtocol,
                SaslPassword = _kafkaOptions.SaslPassword,
            };

            _consumer = new ConsumerBuilder<Ignore, byte[]>(consumerConfig).Build();

            if (_consumer != null)
            {
                Task.Run(
                      async () =>
                      {
                          try
                          {
                              _consumer.Subscribe(topic);
                              while (!_cancellationToken.IsCancellationRequested)
                              {
                                  // This code gets executed sometimes, even though the consumer has been disposed. This results in an "handle is destroyed" exepction
                                  var consumeResult = _consumer.Consume(_cancellationToken);
                                  if (consumeResult.IsPartitionEOF) continue;
                                  await HandleMessage(consumeResult.Message);
                                  _consumer.Commit(consumeResult);
                              }
                          }
                          catch (KafkaException kafkaException)
                          {
                              OnError(kafkaException);
                          }
                          catch (Exception exception)
                          {
                              OnError(exception);
                          }
                      }, _cancellationToken);
            }
            
            return Task.CompletedTask;
        }

        public async Task PublishMessage(string message)
        {
            var producerConfig = new ProducerConfig()
            {
                BootstrapServers = Configuration.ConnectionString,
                SaslMechanism = _kafkaOptions.SaslMechanism,
                SaslPassword = _kafkaOptions.SaslPassword,
                SaslUsername = _kafkaOptions.SaslUsername,
                SecurityProtocol = _kafkaOptions.SecurityProtocol,
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
                    using (_consumer)
                    {
                        _consumer.Unsubscribe();
                        _consumer.Close();
                    }
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

        private async Task HandleMessage(Message<Ignore, byte[]> message)
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