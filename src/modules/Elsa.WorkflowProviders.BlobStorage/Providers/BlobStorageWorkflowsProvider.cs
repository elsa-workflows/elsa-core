using Elsa.WorkflowProviders.BlobStorage.Contracts;
using Elsa.Workflows.Runtime;
using FluentStorage.Blobs;
using JetBrains.Annotations;

namespace Elsa.WorkflowProviders.BlobStorage.Providers;

/// <summary>
/// A workflow definition provider that loads workflow definitions from a storage using FluentStorage (See https://github.com/robinrodricks/FluentStorage).
/// </summary>
[PublicAPI]
public class BlobStorageWorkflowsProvider : IWorkflowsProvider
{
    private readonly IBlobStorageProvider _blobStorageProvider;
    private readonly IEnumerable<IBlobWorkflowFormatHandler> _handlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageWorkflowsProvider"/> class.
    /// </summary>
    public BlobStorageWorkflowsProvider(
        IBlobStorageProvider blobStorageProvider,
        IEnumerable<IBlobWorkflowFormatHandler> handlers)
    {
        _blobStorageProvider = blobStorageProvider;
        _handlers = handlers;
    }

    /// <inheritdoc />
    public string Name => "FluentStorage";

    /// <inheritdoc />
    public async ValueTask<IEnumerable<MaterializedWorkflow>> GetWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        var options = new ListOptions
        {
            Recurse = true,
            BrowseFilter = _ => true // Let handlers decide what they can process
        };

        var blobStorage = _blobStorageProvider.GetBlobStorage();
        var blobs = await blobStorage.ListFilesAsync(options, cancellationToken);
        var results = new List<MaterializedWorkflow>();

        foreach (var blob in blobs)
        {
            var materialized = await TryReadWorkflowAsync(blob, cancellationToken);

            if (materialized != null)
                results.Add(materialized);
        }

        return results;
    }

    private async Task<MaterializedWorkflow?> TryReadWorkflowAsync(Blob blob, CancellationToken cancellationToken)
    {
        var blobStorage = _blobStorageProvider.GetBlobStorage();
        var content = await blobStorage.ReadTextAsync(blob.FullPath, cancellationToken: cancellationToken);

        var extension = Path.GetExtension(blob.FullPath).TrimStart('.');
        var contentType = blob.Properties.TryGetValue("ContentType", out var ct) ? ct?.ToString() : null;

        foreach (var handler in _handlers)
        {
            if (!handler.CanHandle(blob, contentType, extension))
                continue;

            var result = await handler.TryParseAsync(blob, content, cancellationToken);
            if (result != null)
                return result;
        }

        // No handler accepted this blob; ignore it
        return null;
    }
}