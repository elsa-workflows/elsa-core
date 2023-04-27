using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

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
            new WorkflowIdentity(source.DefinitionId, source.Version, source.Id),
            new WorkflowPublication(source.IsLatest, source.IsPublished),
            new WorkflowMetadata(source.Name, source.Description, source.CreatedAt),
            source.Options,
            root,
            source.Variables,
            source.CustomProperties);
    }
    
    /// <summary>
    /// Maps a <see cref="WorkflowDefinition"/> to a <see cref="Workflow"/>.
    /// </summary>
    /// <param name="workflowDefinition">The source <see cref="WorkflowDefinition"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The mapped <see cref="Workflow"/>.</returns>
    public async Task<WorkflowDefinitionModel> MapAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var variables = _variableDefinitionMapper.Map(workflow.Variables).ToList();

        return new(
            workflowDefinition.Id,
            workflowDefinition.DefinitionId,
            workflowDefinition.Name,
            workflowDefinition.Description,
            workflowDefinition.CreatedAt,
            workflowDefinition.Version,
            variables,
            workflowDefinition.Inputs,
            workflowDefinition.Outputs,
            workflowDefinition.Outcomes,
            workflowDefinition.CustomProperties,
            workflowDefinition.UsableAsActivity,
            workflowDefinition.IsLatest,
            workflowDefinition.IsPublished,
            workflow.Options,
            workflow.Root);
    }
}