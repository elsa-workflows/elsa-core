using Confluent.Kafka;
using Elsa.Expressions.Helpers;

namespace Elsa.Kafka.Implementations;

public class ProducerProxy(object producer) : IProducer
{
    private object Producer { get; } = producer;

    public async Task ProduceAsync(string topic, object? key, object value, Headers? headers = null, CancellationToken cancellationToken = default)
    {
        var producerType = Producer.GetType();
        var keyType = producerType.GetGenericArguments()[0];
        var valueType = producerType.GetGenericArguments()[1];
        var messageType = typeof(Message<,>).MakeGenericType(keyType, valueType);
        var produceAsyncMethod = producerType.GetMethod("ProduceAsync", [typeof(string), messageType, typeof(CancellationToken)])!;
        var messageInstance = Activator.CreateInstance(messageType);
        var convertedValue = value.ConvertTo(valueType);

        messageType.GetProperty("Value")!.SetValue(messageInstance, convertedValue);
        if (key != null) messageType.GetProperty("Key")!.SetValue(messageInstance, key);
        if (headers != null) messageType.GetProperty("Headers")!.SetValue(messageInstance, headers);

        await (Task)produceAsyncMethod.Invoke(Producer, [topic, messageInstance, cancellationToken])!;

        var flushMethod = producerType.GetMethod("Flush", [typeof(CancellationToken)])!;
        flushMethod.Invoke(Producer, [cancellationToken]);
    }

    public void Dispose()
    {
        ((IDisposable)Producer).Dispose();
    }
}