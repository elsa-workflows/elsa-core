namespace Elsa.Workflows.Management.BlobStorage.Options;

public sealed class BlobPayloadStorageOptions
{
    public string? ConnectionString { get; set; }

    public string? BaseUrl { get; set; }

    public string? FolderPath { get; set; }

    public string? TypeIdentifier { get; set; }
}
