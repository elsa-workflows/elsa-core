namespace Elsa.Workflows.Management.BlobStorage.Options;

public sealed class BlobPayloadFeatureOptions
{
    public string? ConnectionString { get; set; }

    public string? BaseUrl { get; set; }

    public string? FolderPath { get; set; }

    public BlobPayloadStorageType StorageType { get; set; }

    public string? TypeIdentifier { get; set; }
}
