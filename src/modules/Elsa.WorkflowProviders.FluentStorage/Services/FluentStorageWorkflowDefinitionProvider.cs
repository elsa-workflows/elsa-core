using Elsa.Common.Contracts;
using Elsa.WorkflowProviders.FluentStorage.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using FluentStorage.Blobs;
using JetBrains.Annotations;

namespace Elsa.WorkflowProviders.FluentStorage.Services;

/// <summary>
/// A workflow definition provider that loads workflow definitions from a storage using FluentStorage (See https://github.com/robinrodricks/FluentStorage).
/// </summary>
[PublicAPI]
public class FluentStorageWorkflowDefinitionProvider : IWorkflowDefinitionProvider
{
    private readonly IBlobStorageProvider _blobStorageProvider;
    private readonly IActivitySerializer _activitySerializer;
    private readonly ISystemClock _systemClock;
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentStorageWorkflowDefinitionProvider"/> class.
    /// </summary>
    public FluentStorageWorkflowDefinitionProvider(
        IBlobStorageProvider blobStorageProvider, 
        IActivitySerializer activitySerializer, 
        ISystemClock systemClock, 
        WorkflowDefinitionMapper workflowDefinitionMapper,
        VariableDefinitionMapper variableDefinitionMapper)
    {
        _blobStorageProvider = blobStorageProvider;
        _activitySerializer = activitySerializer;
        _systemClock = systemClock;
        _workflowDefinitionMapper = workflowDefinitionMapper;
        _variableDefinitionMapper = variableDefinitionMapper;
    }
    
    /// <inheritdoc />
    public string Name => "FluentStorage";

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowDefinitionResult>> GetWorkflowDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var options = new ListOptions
        {
            Recurse = true,
        };
        
        var blobStorage = _blobStorageProvider.GetBlobStorage();
        var blobs = await blobStorage.ListFilesAsync(options, cancellationToken);
        var results = new List<WorkflowDefinitionResult>();

        foreach (var blob in blobs)
        {
            var result = await ReadWorkflowDefinitionAsync(blob, cancellationToken);
            results.Add(result);
        }
        
        return results;
    }
    
    private async Task<WorkflowDefinitionResult> ReadWorkflowDefinitionAsync(Blob blob, CancellationToken cancellationToken)
    {
        var blobStorage = _blobStorageProvider.GetBlobStorage();
        var workflowJson = await blobStorage.ReadTextAsync(blob.FullPath, cancellationToken: cancellationToken);
        var workflowDefinitionModel = _activitySerializer.Deserialize<WorkflowDefinitionModel>(workflowJson);
        var variables = _variableDefinitionMapper.Map(workflowDefinitionModel.Variables).ToList();
        var rootJson = _activitySerializer.Serialize(workflowDefinitionModel.Root!);

        var definition = new WorkflowDefinition
        {
            Id = workflowDefinitionModel.Id,
            DefinitionId = workflowDefinitionModel.DefinitionId,
            Version = workflowDefinitionModel.Version,
            Name = workflowDefinitionModel.Name,
            Description = workflowDefinitionModel.Description,
            CustomProperties = workflowDefinitionModel.CustomProperties ?? new Dictionary<string, object>(),
            Variables = variables,
            IsLatest = workflowDefinitionModel.IsLatest,
            IsPublished = workflowDefinitionModel.IsPublished,
            CreatedAt = workflowDefinitionModel.CreatedAt == default ? _systemClock.UtcNow : workflowDefinitionModel.CreatedAt,
            MaterializerName = JsonWorkflowMaterializer.MaterializerName,
            StringData = rootJson
        };

        var workflow = _workflowDefinitionMapper.Map(definition);

        return new WorkflowDefinitionResult(definition, workflow);
    }    
}