using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Management.Payloads;

public sealed class WorkflowPayloadStoreManager(
    IEnumerable<IWorkflowPayloadStore> payloadStores,
    ILogger<WorkflowPayloadStoreManager> logger) 

    : IWorkflowPayloadStoreManager
{
    public ValueTask<string?> Get(PayloadReference payloadReference, CancellationToken cancellationToken)
    {
        var payloadStore = payloadStores.FirstOrDefault(x => x.Type == payloadReference.Type);
        if (payloadStore is null)
        {
            LogWarningPayloadStoreNotFound(payloadReference.Type);
            return new(default(string));
        }

        try
        {
            return payloadStore.Get(payloadReference.Url, cancellationToken)!;
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
                logger.LogError(e, "Failed to retrieve payload for reference '{uri}' and type '{type}'", payloadReference.Url, payloadReference.Type);

            return new(default(string));
        }
    }

    public async ValueTask<PayloadReference?> Set(string entityId, string data, string payloadType, CancellationToken cancellationToken)
    {
        var payloadStore = payloadStores.FirstOrDefault(x => x.Type == payloadType);
        if (payloadStore is null)
        {
            LogWarningPayloadStoreNotFound(payloadType);
            return null;
        }

        try
        {
            var url = await payloadStore.Set(entityId, data, cancellationToken);
            return new PayloadReference(url.ToString(), payloadType);
        }
        catch (Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError(e, "Failed to upload to payload store. Error: {msg}", e.Message);

            return null;
        }
    }    

    private void LogWarningPayloadStoreNotFound(string type)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(
                "Could not find payload store for data reference type '{t}'. Hence, loading the data from the database",
                type
            );
        }
    }
}
