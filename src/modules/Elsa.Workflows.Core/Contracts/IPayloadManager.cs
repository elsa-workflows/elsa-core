using Elsa.Workflows.Payloads;

namespace Elsa.Workflows.Contracts;

public interface IPayloadManager
{
    ValueTask<string?> Get(PayloadReference payloadReference, CancellationToken cancellationToken);

    ValueTask<PayloadReference?> Set(string name, string data, string type, string? compressionAlgorithm, CancellationToken cancellationToken);
}
