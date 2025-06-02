using Elsa.Common;
using Elsa.WorkflowProviders.BlobStorage.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
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
    private readonly IActivitySerializer _activitySerializer;
    private readonly ISystemClock _systemClock;
    private readonly IHasher _hasher;
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageWorkflowsProvider"/> class.
    /// </summary>
    public BlobStorageWorkflowsProvider(
        IBlobStorageProvider blobStorageProvider,
        IActivitySerializer activitySerializer,
        ISystemClock systemClock,
        IHasher hasher,
        WorkflowDefinitionMapper workflowDefinitionMapper,
        VariableDefinitionMapper variableDefinitionMapper)
    {
        _blobStorageProvider = blobStorageProvider;
        _activitySerializer = activitySerializer;
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
            BrowseFilter = x => x.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || x.Name.EndsWith(".elsa", StringComparison.OrdinalIgnoreCase)
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

        throw new NotSupportedException($"The file extension '{fileExtension}' is not supported.");
    }

    private MaterializedWorkflow ReadJsonWorkflowDefinition(string json)
    {
        var workflowDefinitionModel = _activitySerializer.Deserialize<WorkflowDefinitionModel>(json);
        var workflow = _workflowDefinitionMapper.Map(workflowDefinitionModel);

        return new (workflow, Name, JsonWorkflowMaterializer.MaterializerName);
    }
}