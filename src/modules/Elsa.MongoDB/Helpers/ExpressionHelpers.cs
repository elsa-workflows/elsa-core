using System.Linq.Expressions;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.MongoDB.Helpers;

public class ExpressionHelpers
{
    public static readonly Expression<Func<WorkflowDefinition, WorkflowDefinitionSummary>> WorkflowDefinitionSummary = 
        workflowDefinition => new WorkflowDefinitionSummary
        (
            workflowDefinition.Id,
            workflowDefinition.DefinitionId,
            workflowDefinition.Name,
            workflowDefinition.Description,
            workflowDefinition.Version,
            workflowDefinition.IsLatest,
            workflowDefinition.IsPublished,
            workflowDefinition.MaterializerName,
            workflowDefinition.CreatedAt
        );
    
    public static readonly Expression<Func<WorkflowInstance, WorkflowInstanceSummary>> WorkflowInstanceSummary = 
        workflowInstance => new WorkflowInstanceSummary
        (
            workflowInstance.Id,
            workflowInstance.DefinitionId,
            workflowInstance.DefinitionVersionId,
            workflowInstance.Version,
            workflowInstance.Status,
            workflowInstance.SubStatus,
            workflowInstance.CorrelationId,
            workflowInstance.Name,
            workflowInstance.CreatedAt,
            workflowInstance.LastExecutedAt,
            workflowInstance.FinishedAt,
            workflowInstance.CancelledAt,
            workflowInstance.FaultedAt
        );
}