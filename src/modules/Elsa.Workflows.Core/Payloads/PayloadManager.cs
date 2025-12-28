using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Common;
using Elsa.Common.Codecs;
using Elsa.Common.Services;
using Elsa.Workflows.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Payloads;

public sealed class PayloadManager(IEnumerable<IPayloadStorage> payloadStores, ICompressionCodecResolver compressionCodecResolver,ILogger<PayloadManager> logger) 
    : IPayloadManager
{
    public async ValueTask<string?> Get(PayloadReference payloadReference, CancellationToken cancellationToken)
    {
        var payloadStore = payloadStores.FirstOrDefault(x => x.TypeIdentifier == payloadReference.TypeIdentifier);
        if (payloadStore is null)
        {
            LogWarningPayloadStoreNotFound(payloadReference.TypeIdentifier);
            return new(default(string));
        }

        try
        {
            var url = new Uri(payloadReference.Url);
            var payload = await payloadStore.Get(url, cancellationToken)!;
            var compressionStrategy = compressionCodecResolver.Resolve(payloadReference.CompressionAlgorithm ?? nameof(None));
            return await compressionStrategy.DecompressAsync(payload, cancellationToken);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
                logger.LogError(e, "Failed to retrieve payload for reference '{uri}' and type '{type}'", payloadReference.Url, payloadReference.TypeIdentifier);

            return new(default(string));
        }
    }

    public async ValueTask<PayloadReference?> Set(string name, string data, string type, string? compressionAlgorithm, CancellationToken cancellationToken)
    {
        compressionAlgorithm ??= nameof(None);

        var payloadStore = payloadStores.FirstOrDefault(x => x.TypeIdentifier == type);
        if (payloadStore is null)
        {
            LogWarningPayloadStoreNotFound(type);
            return null;
        }

        try
        {            
            var compressionCodec = compressionCodecResolver.Resolve(compressionAlgorithm);
            var compressedJson = await compressionCodec.CompressAsync(data, cancellationToken);

            var url = await payloadStore.Set(name, data, cancellationToken);
            return new PayloadReference(url.ToString(), type, compressionAlgorithm);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
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
