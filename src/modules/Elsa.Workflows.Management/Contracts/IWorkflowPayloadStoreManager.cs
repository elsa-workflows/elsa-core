using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Contracts;

public interface IWorkflowPayloadStoreManager
{
    ValueTask<string?> Get(PayloadReference payloadReference, CancellationToken cancellationToken);

    ValueTask<PayloadReference?> Set(string entityId, string data, string payloadType, CancellationToken cancellationToken);
}
