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
public class JsonBlobWorkflowFormatHandler : IBlobWorkflowFormatHandler
{
    private readonly IActivitySerializer _activitySerializer;
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;
    private readonly ILogger<JsonBlobWorkflowFormatHandler> _logger;

    public JsonBlobWorkflowFormatHandler(
        IActivitySerializer activitySerializer,
        WorkflowDefinitionMapper workflowDefinitionMapper,
        ILogger<JsonBlobWorkflowFormatHandler> logger)
    {
        _activitySerializer = activitySerializer;
        _workflowDefinitionMapper = workflowDefinitionMapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Json";

    /// <inheritdoc />
    public bool CanHandle(Blob blob, string? contentType, string? extension)
    {
        if (!string.IsNullOrEmpty(extension) && extension.Equals("json", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrEmpty(contentType) && contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <inheritdoc />
    public ValueTask<MaterializedWorkflow?> TryParseAsync(Blob blob, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            var workflowDefinitionModel = _activitySerializer.Deserialize<WorkflowDefinitionModel>(content);
            var workflow = _workflowDefinitionMapper.Map(workflowDefinitionModel);

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
            // Log the error for troubleshooting
            _logger.LogWarning(ex, "Failed to parse JSON workflow from blob '{BlobPath}'. The file will be skipped.", blob.FullPath);

            // Return null to indicate this handler can't process the content
            return new((MaterializedWorkflow?)null);
        }
    }
}
