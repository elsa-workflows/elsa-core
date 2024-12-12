using System.Text;
using Microsoft.Extensions.Options;

namespace Elsa.Kafka;

/// <summary>
/// A correlation strategy that retrieves a correlation ID from a specified header in a Kafka transport message.
/// </summary>
public class HeaderCorrelationStrategy(IOptions<KafkaOptions> options) : ICorrelationStrategy
{
    /// <inheritdoc/>
    public string? GetCorrelationId(KafkaTransportMessage transportMessage)
    {
        var headerName = options.Value.CorrelationHeaderKey;
        return !transportMessage.Headers.TryGetValue(headerName, out var headerValue)
            ? null
            : Encoding.UTF8.GetString(headerValue);
    }
}