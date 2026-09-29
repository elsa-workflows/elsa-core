using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Represents the workflow definition version record.
/// </summary>
public record WorkflowDefinitionVersion(string WorkflowDefinitionId, string WorkflowDefinitionVersionId, int Version)
{
    /// <summary>
    /// Performs the from definition operation.
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition.</param>
    /// <returns>The result of the operation.</returns>
    public static WorkflowDefinitionVersion FromDefinition(WorkflowDefinition workflowDefinition)
    {
        return new(workflowDefinition.DefinitionId, workflowDefinition.Id, workflowDefinition.Version);
    }
    
    /// <summary>
    /// Performs the from definition summary operation.
    /// </summary>
    /// <param name="workflowDefinitionSummary">The workflow definition summary.</param>
    /// <returns>The result of the operation.</returns>
    public static WorkflowDefinitionVersion FromDefinitionSummary(WorkflowDefinitionSummary workflowDefinitionSummary)
    {
        return new(workflowDefinitionSummary.DefinitionId, workflowDefinitionSummary.Id, workflowDefinitionSummary.Version);
    }

    /// <summary>
    /// Performs the from definition model operation.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The result of the operation.</returns>
    public static WorkflowDefinitionVersion FromDefinitionModel(WorkflowDefinitionModel model)
    {
        return new(model.DefinitionId, model.Id, model.Version);
    }
}