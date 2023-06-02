using System.Text.Json;
using Elsa.Common.Contracts;
using Elsa.Dsl.Contracts;
using Elsa.Extensions;
using Elsa.WorkflowProviders.FluentStorage.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using FluentStorage.Blobs;
using JetBrains.Annotations;

namespace Elsa.WorkflowProviders.FluentStorage.Providers;

/// <summary>
/// A workflow definition provider that loads workflow definitions from a storage using FluentStorage (See https://github.com/robinrodricks/FluentStorage).
/// </summary>
[PublicAPI]
public class FluentStorageWorkflowProvider : IWorkflowProvider
{
    private readonly IBlobStorageProvider _blobStorageProvider;
    private readonly IActivitySerializer _activitySerializer;
    private readonly IDslEngine _dslEngine;
    private readonly ISystemClock _systemClock;
    private readonly IHasher _hasher;
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentStorageWorkflowProvider"/> class.
    /// </summary>
    public FluentStorageWorkflowProvider(
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
        var jsonBlobs = blobs.Where(x => x.FullPath.EndsWith(".json"));
        var dslBlobs = blobs.Where(x => x.FullPath.EndsWith(".elsa"));
        var results = new List<MaterializedWorkflow>();

        var jsonWorkflowDescriptors = await ReadOrderedJsonBlobsAsync(jsonBlobs, cancellationToken);
        foreach (var workflowDescriptor in jsonWorkflowDescriptors)
        {
            var result = ReadJsonWorkflowDefinition(workflowDescriptor.JsonDocument);
            results.Add(result);
        }

        foreach (var blob in dslBlobs)
        {
            var result = await ReadElsaDslWorkflowDefinitionAsync(blob, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    private async Task<MaterializedWorkflow> ReadElsaDslWorkflowDefinitionAsync(Blob blob, CancellationToken cancellationToken)
    {
        var blobStorage = _blobStorageProvider.GetBlobStorage();
        var dsl = await blobStorage.ReadTextAsync(blob.FullPath, cancellationToken: cancellationToken);
        var workflow = await _dslEngine.ParseAsync(dsl, cancellationToken);

        // TODO: Extend the DSL with support for setting the ID from there.
        workflow.Identity = workflow.Identity with
        {
            Id = blob.Name,
            DefinitionId = blob.Name
        };

        return new MaterializedWorkflow(workflow, JsonWorkflowMaterializer.MaterializerName);
    }

    private async Task<IEnumerable<WorkflowDescriptor>> ReadOrderedJsonBlobsAsync(IEnumerable<Blob> blobs, CancellationToken cancellationToken)
    {
        var blobStorage = _blobStorageProvider.GetBlobStorage();
        
        var jsonDocuments = await Task.WhenAll(blobs.Select(async blob =>
        {
            var json = await blobStorage.ReadTextAsync(blob.FullPath, cancellationToken: cancellationToken);
            var jsonDocument = JsonDocument.Parse(json);
            return jsonDocument;
        }));
        
        // Create a dictionary of JSON documents by their ID property.
        var jsonDocumentsById = jsonDocuments.ToDictionary(x => x.RootElement.GetProperty("id").GetString()!);
        
        var workflowDescriptors = new List<WorkflowDescriptor>();

        foreach (var jsonDocument in jsonDocuments)
        {
            var definitionId = jsonDocument.RootElement.GetProperty("definitionId").GetString()!;
            var definitionVersionId = jsonDocument.RootElement.GetProperty("id").GetString()!;
            var workflowDescriptor = new WorkflowDescriptor { DefinitionId = definitionId, DefinitionVersionId = definitionVersionId, JsonDocument = jsonDocument};

            // Find all workflow definition activities (child workflows) and note them as dependencies.
            var activityElements = EnumerateActivities(jsonDocument.RootElement);

            // For each activity element, check if it contains a workflow definition ID and a workflow definition version ID.
            // If so, add it as a dependency.

            foreach (var activityElement in activityElements)
            {
                if (!activityElement.TryGetProperty("workflowDefinitionId", out var workflowDefinitionIdProperty))
                    continue;

                if (!activityElement.TryGetProperty("workflowDefinitionVersionId", out var workflowDefinitionVersionIdProperty))
                    continue;

                var workflowDefinitionId = workflowDefinitionIdProperty.GetString()!;
                var workflowDefinitionVersionId = workflowDefinitionVersionIdProperty.GetString()!;

                var dependency = new WorkflowDescriptor
                {
                    DefinitionId = workflowDefinitionId,
                    DefinitionVersionId = workflowDefinitionVersionId,
                    JsonDocument = jsonDocumentsById[workflowDefinitionVersionId]
                };

                workflowDescriptor.Dependencies.Add(dependency);
            }

            workflowDescriptors.Add(workflowDescriptor);
        }

        var sortedWorkflowDescriptors = workflowDescriptors.TSort(x => x.Dependencies);

        return sortedWorkflowDescriptors.DistinctBy(x => x.DefinitionVersionId);
    }

    private static IEnumerable<JsonElement> EnumerateActivities(JsonElement element)
    {
        var activities = new List<JsonElement>();

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                foreach (var property in element.EnumerateObject())
                    if (property is { Name: "activities", Value.ValueKind: JsonValueKind.Array })
                    {
                        foreach (var activity in property.Value.EnumerateArray())
                        {
                            activities.Add(activity);
                            activities.AddRange(EnumerateActivities(activity));
                        }
                    }
                    else
                        activities.AddRange(EnumerateActivities(property.Value));

                break;
            }
            case JsonValueKind.Array:
            {
                foreach (var item in element.EnumerateArray()) 
                    activities.AddRange(EnumerateActivities(item));

                break;
            }
        }

        return activities;
    }

    private MaterializedWorkflow ReadJsonWorkflowDefinition(JsonDocument jsonDocument)
    {
        var json = jsonDocument.RootElement.ToString()!;
        var workflowDefinitionModel = _activitySerializer.Deserialize<WorkflowDefinitionModel>(json);
        var workflow = _workflowDefinitionMapper.Map(workflowDefinitionModel);
        
        workflow.Options.UsableAsActivity = workflowDefinitionModel.UsableAsActivity ?? false;
        return new MaterializedWorkflow(workflow, JsonWorkflowMaterializer.MaterializerName);
    }
}

internal class WorkflowDescriptor
{
    public string DefinitionId { get; set; } = default!;
    public string DefinitionVersionId { get; set; } = default!;
    public JsonDocument JsonDocument { get; set; } = default!;
    public ICollection<WorkflowDescriptor> Dependencies { get; set; } = new List<WorkflowDescriptor>();
}