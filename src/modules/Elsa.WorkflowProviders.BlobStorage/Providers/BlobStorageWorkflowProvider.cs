using Elsa.Common.Contracts;
using Elsa.Dsl.Contracts;
using Elsa.WorkflowProviders.BlobStorage.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using FluentStorage.Blobs;
using JetBrains.Annotations;

namespace Elsa.WorkflowProviders.BlobStorage.Providers;

/// <summary>
/// A workflow definition provider that loads workflow definitions from a storage using FluentStorage (See https://github.com/robinrodricks/FluentStorage).
/// </summary>
[PublicAPI]
public class BlobStorageWorkflowProvider : IWorkflowProvider
{
    private readonly IBlobStorageProvider _blobStorageProvider;
    private readonly IActivitySerializer _activitySerializer;
    private readonly IDslEngine _dslEngine;
    private readonly ISystemClock _systemClock;
    private readonly IHasher _hasher;
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageWorkflowProvider"/> class.
    /// </summary>
    public BlobStorageWorkflowProvider(
        IBlobStorageProvider blobStorageProvider,
        IActivitySerializer activitySerializer,
        IDslEngine dslEngine,
        ISystemClock systemClock,
        IHasher hasher,
        WorkflowDefinitionMapper workflowDefinitionMapper,
        VariableDefinitionMapper variableDefinitionMapper)
    {
        _blobStorageProvider = blobStorageProvider;
        _activitySerializer = activitySerializer;
        _dslEngine = dslEngine;
        _systemClock = systemClock;
        _hasher = hasher;
        _workflowDefinitionMapper = workflowDefinitionMapper;
        _variableDefinitionMapper = variableDefinitionMapper;
    }

    /// <inheritdoc />
    public string Name => "FluentStorage";

    /// <inheritdoc />
    public async ValueTask<IEnumerable<MaterializedWorkflow>> GetWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        var options = new ListOptions
        {
            Recurse = true,
        };

        var blobStorage = _blobStorageProvider.GetBlobStorage();
        var blobs = await blobStorage.ListFilesAsync(options, cancellationToken);
        var results = new List<MaterializedWorkflow>();

        foreach (var blob in blobs)
        {
            var result = await ReadWorkflowAsync(blob, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    private async Task<MaterializedWorkflow> ReadWorkflowAsync(Blob blob, CancellationToken cancellationToken)
    {
        var blobStorage = _blobStorageProvider.GetBlobStorage();
        var fileExtension = blob.FullPath.Split('.').Last();
        var data = await blobStorage.ReadTextAsync(blob.FullPath, cancellationToken: cancellationToken);

        if (string.Equals("json", fileExtension, StringComparison.OrdinalIgnoreCase))
            return ReadJsonWorkflowDefinition(data);

        if (string.Equals("elsa", fileExtension, StringComparison.OrdinalIgnoreCase))
            return await ReadElsaDslWorkflowDefinitionAsync(blob, data, cancellationToken);

        throw new NotSupportedException($"The file extension '{fileExtension}' is not supported.");
    }

    private async Task<MaterializedWorkflow> ReadElsaDslWorkflowDefinitionAsync(Blob blob, string dsl, CancellationToken cancellationToken)
    {
        var workflow = await _dslEngine.ParseAsync(dsl, cancellationToken);

        // TODO: Extend the DSL with support for setting the ID from there.
        workflow.Identity = workflow.Identity with
        {
            Id = blob.Name,
            DefinitionId = blob.Name
        };

        return new MaterializedWorkflow(workflow, Name, JsonWorkflowMaterializer.MaterializerName);
    }

    private MaterializedWorkflow ReadJsonWorkflowDefinition(string json)
    {
        var workflowDefinitionModel = _activitySerializer.Deserialize<WorkflowDefinitionModel>(json);
        var workflow = _workflowDefinitionMapper.Map(workflowDefinitionModel);

        return new MaterializedWorkflow(workflow, Name, JsonWorkflowMaterializer.MaterializerName);
    }
}