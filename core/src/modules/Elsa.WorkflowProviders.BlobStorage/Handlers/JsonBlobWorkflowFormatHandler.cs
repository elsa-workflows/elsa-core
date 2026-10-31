using Elsa.WorkflowProviders.BlobStorage.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime;
using FluentStorage.Blobs;
using Microsoft.Extensions.Logging;

namespace Elsa.WorkflowProviders.BlobStorage.Handlers;

/// <summary>
/// Handles JSON-formatted workflow definitions from blob storage.
/// </summary>
public class JsonBlobWorkflowFormatHandler(
    IActivitySerializer activitySerializer,
    WorkflowDefinitionMapper workflowDefinitionMapper,
    ILogger<JsonBlobWorkflowFormatHandler> logger) : IBlobWorkflowFormatHandler
{
    /// <inheritdoc />
    public string Name => "Json";

    /// <inheritdoc />
    public IEnumerable<string> SupportedExtensions => ["json"];

    /// <inheritdoc />
    public bool CanHandle(Blob blob, string? contentType)
    {
        // Extension filtering is already handled by the provider via SupportedExtensions.
        // Here we can optionally check content type for additional validation.
        if (!string.IsNullOrEmpty(contentType) && contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            return true;

        // If no content type is available, assume we can handle it (since extension was already validated)
        return true;
    }

    /// <inheritdoc />
    public ValueTask<MaterializedWorkflow?> TryParseAsync(Blob blob, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            var workflowDefinitionModel = activitySerializer.Deserialize<WorkflowDefinitionModel>(content);
            var workflow = workflowDefinitionMapper.Map(workflowDefinitionModel);

            var materialized = new MaterializedWorkflow(
                workflow,
                ProviderName: "FluentStorage",
                MaterializerName: JsonWorkflowMaterializer.MaterializerName,
                MaterializerContext: null,
                OriginalSource: content // Preserve full JSON for symmetric round-trip
            );

            return new(materialized);
        }
        catch (Exception ex)
        {
            // Intentionally catching all exceptions here to gracefully handle any failure during workflow parsing.
            // This includes JsonException, IOException, deserialization errors, or any unexpected exceptions.
            // Since this is a format handler for user-provided files, we want to log and skip invalid files
            // rather than crashing the workflow loading process.
            logger.LogWarning(ex, "Failed to parse JSON workflow from blob '{BlobPath}'. The file will be skipped.", blob.FullPath);

            // Return null to indicate this handler can't process the content
            return new((MaterializedWorkflow?)null);
        }
    }
}
