using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Mappers;

/// <summary>
/// Maps <see cref="WorkflowDefinition"/> to and from <see cref="Workflow"/> and <see cref="WorkflowDefinitionModel"/>.
/// </summary>
public class WorkflowDefinitionMapper
{
    private readonly IActivitySerializer _activitySerializer;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowDefinitionMapper"/> class.
    /// </summary>
    public WorkflowDefinitionMapper(IActivitySerializer activitySerializer, IWorkflowDefinitionService workflowDefinitionService, VariableDefinitionMapper variableDefinitionMapper)
    {
        _activitySerializer = activitySerializer;
        _workflowDefinitionService = workflowDefinitionService;
        _variableDefinitionMapper = variableDefinitionMapper;
    }

    /// <summary>
    /// Maps a <see cref="WorkflowDefinition"/> to a <see cref="Workflow"/>.
    /// </summary>
    /// <param name="source">The source <see cref="WorkflowDefinition"/>.</param>
    /// <returns>The mapped <see cref="Workflow"/>.</returns>
    public Workflow Map(WorkflowDefinition source)
    {
        var root = _activitySerializer.Deserialize(source.StringData!);

        return new(
            new(source.DefinitionId, source.Version, source.Id, source.TenantId),
            new(source.IsLatest, source.IsPublished),
            new(source.Name, source.Description, source.CreatedAt, source.ToolVersion),
            source.Options,
            root,
            source.Variables,
            source.Inputs,
            source.Outputs,
            source.Outcomes,
            source.CustomProperties,
            source.IsReadonly,
            source.IsSystem);
    }

    /// <summary>
    /// Maps a <see cref="WorkflowDefinitionModel"/> to a <see cref="Workflow"/>.
    /// </summary>
    /// <param name="source">The source <see cref="WorkflowDefinitionModel"/>.</param>
    /// <returns>The mapped <see cref="Workflow"/>.</returns>
    public Workflow Map(WorkflowDefinitionModel source)
    {
        var root = source.Root!;
        var variables = _variableDefinitionMapper.Map(source.Variables).ToList();
        var options = source.Options ?? new WorkflowOptions();

        // TODO: Remove this in the future when users have migrated workflows to use the new UsableAsActivity options property.

#pragma warning disable CS0618
        options.UsableAsActivity ??= source.UsableAsActivity ?? false;
#pragma warning restore CS0618

        return new(
            new(source.DefinitionId, source.Version, source.Id, source.TenantId),
            new(source.IsLatest, source.IsPublished),
            new(source.Name, source.Description, source.CreatedAt, source.ToolVersion),
            options,
            root,
            variables,
            source.Inputs ?? new List<InputDefinition>(),
            source.Outputs ?? new List<OutputDefinition>(),
            source.Outcomes ?? new List<string>(),
            source.CustomProperties ?? new Dictionary<string, object>(),
            source.IsReadonly,
            source.IsSystem);
    }
    
    public WorkflowDefinition MapToWorkflowDefinition(WorkflowDefinitionModel source)
    {
        var root = source.Root!;
        var variables = _variableDefinitionMapper.Map(source.Variables).ToList();
        var options = source.Options ?? new WorkflowOptions();
        var stringData = _activitySerializer.Serialize(root);

        return new()
        {
            IsPublished = source.IsPublished,
            Description = source.Description,
            Id = source.Id,
            Inputs = source.Inputs ?? [],
            Name = source.Name,
            Options = options,
            Outcomes = source.Outcomes ?? [],
            Outputs = source.Outputs ?? [],
            Variables = variables,
            Version = source.Version,
            CreatedAt = source.CreatedAt,
            CustomProperties = source.CustomProperties ?? new Dictionary<string, object>(),
            DefinitionId = source.DefinitionId,
            IsLatest = source.IsLatest,
            IsReadonly = source.IsReadonly,
            IsSystem = source.IsSystem,
            TenantId = source.TenantId,
            ToolVersion = source.ToolVersion,
            StringData = stringData,
            MaterializerName = JsonWorkflowMaterializer.MaterializerName
        };
    }

    /// <summary>
    /// Maps many <see cref="WorkflowDefinition"/>s to many <see cref="WorkflowDefinitionModel"/>s.
    /// </summary>
    /// <param name="source">The source <see cref="WorkflowDefinition"/>s.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The mapped <see cref="WorkflowDefinitionModel"/>s.</returns>
    public async Task<IEnumerable<WorkflowDefinitionModel>> MapAsync(IEnumerable<WorkflowDefinition> source, CancellationToken cancellationToken = default)
    {
        return await Task.WhenAll(source.Select(async x => await MapAsync(x, cancellationToken)));
    }

    /// <summary>
    /// Maps a <see cref="WorkflowDefinition"/> to a <see cref="Workflow"/>.
    /// </summary>
    /// <param name="workflowDefinition">The source <see cref="WorkflowDefinition"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The mapped <see cref="Workflow"/>.</returns>
    public async Task<WorkflowDefinitionModel> MapAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
    {
        var workflowGraph = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var workflow = workflowGraph.Workflow;
        var variables = _variableDefinitionMapper.Map(workflow.Variables).ToList();

        return new(
            workflowDefinition.Id,
            workflowDefinition.DefinitionId,
            workflowDefinition.TenantId,
            workflowDefinition.Name,
            workflowDefinition.Description,
            workflowDefinition.CreatedAt,
            workflowDefinition.Version,
            workflowDefinition.ToolVersion,
            variables,
            workflowDefinition.Inputs,
            workflowDefinition.Outputs,
            workflowDefinition.Outcomes,
            workflowDefinition.CustomProperties,
            workflowDefinition.IsReadonly,
            workflowDefinition.IsSystem,
            workflowDefinition.IsLatest,
            workflowDefinition.IsPublished,
            workflow.Options,
            null,
            workflow.Root);
    }

    /// <summary>
    /// Maps a <see cref="Workflow"/> to a <see cref="WorkflowDefinitionModel"/>.
    /// </summary>
    /// <param name="workflow">The source <see cref="WorkflowDefinition"/>.</param>
    /// <returns>The mapped <see cref="WorkflowDefinitionModel"/>.</returns>
    public WorkflowDefinitionModel Map(Workflow workflow)
    {
        var variables = _variableDefinitionMapper.Map(workflow.Variables).ToList();

        return new(
            workflow.Identity.Id,
            workflow.Identity.DefinitionId,
            workflow.Identity.TenantId,
            workflow.WorkflowMetadata.Name,
            workflow.WorkflowMetadata.Description,
            workflow.WorkflowMetadata.CreatedAt,
            workflow.Identity.Version,
            workflow.WorkflowMetadata.ToolVersion,
            variables,
            workflow.Inputs,
            workflow.Outputs,
            workflow.Outcomes,
            workflow.CustomProperties,
            workflow.IsReadonly,
            workflow.IsSystem,
            workflow.Publication.IsLatest,
            workflow.Publication.IsPublished,
            workflow.Options,
            null,
            workflow.Root);
    }
}