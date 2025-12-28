using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.BlobStorage.Options;
using Elsa.Workflows.Management.Contracts;
using FluentStorage.Blobs;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management.BlobStorage;

public sealed class BlobPayloadStorage(IBlobStorage blobStorage, IOptions<BlobPayloadStorageOptions> options) : IPayloadStorage
{
    public const string DefaultTypeIdentifier = "default-blob";

    public string TypeIdentifier => options.Value.TypeIdentifier ?? DefaultTypeIdentifier;

    public ValueTask<string> Get(Uri url, CancellationToken cancellationToken)
    {
        var path = url
            .ToString()
            .Replace($"{options.Value.BaseUrl}", string.Empty);

        return new(blobStorage.ReadTextAsync(path, cancellationToken: cancellationToken));
    }    

    public async ValueTask<Uri> Set(string name, string data, CancellationToken cancellationToken)
    {
        var fullUrl = GetFullUrl($"{name}.txt");
        var uri = new Uri(fullUrl);
        var path = uri.GetLeftPart(UriPartial.Path);

        await blobStorage.WriteTextAsync(path, data, cancellationToken: cancellationToken);

        return uri;
    }

    private string GetFullUrl(string blobName)
    {
        if (string.IsNullOrWhiteSpace(options.Value.BaseUrl))
            throw new InvalidOperationException($"{nameof(BlobPayloadStorageOptions.BaseUrl)} is not configured for {nameof(BlobPayloadStorageOptions)}.");

        var result = options.Value.BaseUrl.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(options.Value.FolderPath))
        {
            result += $"/{options.Value.FolderPath.Trim('/')}";
        }

        result += $"/{blobName}";
        return result;
    }
}
