using Azure.Storage.Blobs.Specialized;
using Elsa.Storage.AzureStorage.Extensions;
using Elsa.Storage.AzureStorage.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Storage.AzureStorage.Activities;

/// <summary>
/// Represents an activity used to upload a blob to Azure Storage.
/// </summary>
[Activity(
    "Elsa.Integrations.Storage",
    "Storage",
    "Uploads a blob to Azure Storage.",
    DisplayName = "Upload blob", Kind = ActivityKind.Task)]
[UsedImplicitly]
public class UploadBlob : Activity
{
    /// <summary>
    /// The content to upload.
    /// </summary>
    [Input(Description = "The content to upload.")]
    public Input<object> Content { get; set; } = null!;
    
    // TODO: Replace this with the new Connection API that is coming soon.
    /// <summary>
    /// The connection string for the Azure Storage account.
    /// </summary>
    public Input<string> ConnectionString { get; set; } = null!;

    /// <summary>
    /// The name of the blob to upload.
    /// </summary>
    public Input<string> BlobName { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var connectionString = context.Get(ConnectionString)!;
        var blobName = context.Get(BlobName)!;
        var content = context.Get(Content)!;
        var blobContainerClientFactory = context.GetRequiredService<BlobContainerClientFactory>();
        var blockBlobClient = blobContainerClientFactory.GetBlobContainerClient(connectionString).GetBlockBlobClient($"{blobName}.json");
        await blockBlobClient.UploadJsonAsBlocksAsync(content, cancellationToken: cancellationToken);
    }
}
