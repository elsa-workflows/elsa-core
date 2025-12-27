using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Contracts;

public interface IWorkflowPayloadStore
{
    string Type { get; }

    ValueTask<string> Get(string payloadUrl, CancellationToken cancellationToken);

    ValueTask<Uri> Set(string entityId, string data, CancellationToken cancellationToken);
}
