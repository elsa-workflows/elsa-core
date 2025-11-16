using Elsa.WorkflowProviders.BlobStorage.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime;
using FluentStorage.Blobs;

namespace Elsa.WorkflowProviders.BlobStorage.Handlers;

/// <summary>
/// Handles JSON-formatted workflow definitions from blob storage.
/// </summary>
public class JsonBlobWorkflowFormatHandler : IBlobWorkflowFormatHandler
{
    private readonly IActivitySerializer _activitySerializer;
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;

    public JsonBlobWorkflowFormatHandler(
        IActivitySerializer activitySerializer,
        WorkflowDefinitionMapper workflowDefinitionMapper)
    {
        _activitySerializer = activitySerializer;
        _workflowDefinitionMapper = workflowDefinitionMapper;
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
        catch
        {
            // If JSON parsing fails, return null to indicate this handler can't process the content
            return new((MaterializedWorkflow?)null);
        }
    }
}
