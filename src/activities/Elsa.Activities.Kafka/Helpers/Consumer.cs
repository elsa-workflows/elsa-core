using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Reactive = System.Reactive;

namespace Elsa.Activities.Kafka.Helpers
{
    public static class Consumer
    {
        public static IObservable<Message<TKey,TValue>> Consume<TKey, TValue>(
            string topic,
            IConsumer<TKey,TValue> consumer)
        {
            return Reactive.Linq.Observable.Create<Message<TKey,TValue>>(
                (observer, cancellationToken) =>
                {
                    Task.Run(
                        () =>
                        {
                            try
                            {
                                consumer.Subscribe(topic);
                                while (!cancellationToken.IsCancellationRequested)
                                {
                                    var consumeResult = consumer.Consume(cancellationToken);
                                    if (consumeResult.IsPartitionEOF) continue;
                                    consumer.Commit(consumeResult);
                                    observer.OnNext(consumeResult.Message);
                                }
                            }
                            catch (KafkaException kafkaException)
                            {
                                   observer.OnError(kafkaException);
                            }
                            catch (Exception exception)
                            {
                                observer.OnError(exception);
                            }
                        },
                        cancellationToken);

                    // it is important to enforce IDisposable as a return value, not just Task (which is deduced automatically);
                    // otherwise Rx framework will dispose consumer as soon as it gets created
                    return Task.FromResult<IDisposable>(consumer);
                });
        }
    }
}