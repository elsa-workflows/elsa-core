using Elsa.Workflows.Management.Contracts;
using FluentStorage.Blobs;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management.BlobStorage;

public sealed class BlobPayloadStore(IBlobStorage blobStorage, IOptions<BlobPayloadStoreOptions> options) : IWorkflowPayloadStore
{
    public const string TypeName = "default-blob";

    public string Type => TypeName;    

    public ValueTask<string> Get(string url, CancellationToken cancellationToken)
    {
        var uri = new Uri(url);
        var path = uri
            .GetLeftPart(UriPartial.Path)
            .Split('/')
            .Last();
        return new(blobStorage.ReadTextAsync(path, cancellationToken: cancellationToken));
    }

    public async ValueTask<Uri> Set(string entityId, string data, CancellationToken cancellationToken)
    {
        var uri = new Uri($"{options.Value.FolderUrl}/{entityId}.txt");
        var path = uri
            .GetLeftPart(UriPartial.Path)
            .Split('/')
            .Last();
        await blobStorage.WriteTextAsync(path, data, cancellationToken: cancellationToken);
        return uri;
    }
}
