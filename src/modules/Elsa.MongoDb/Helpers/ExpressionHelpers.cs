using System.Linq.Expressions;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.MongoDb.Helpers;

internal class ExpressionHelpers
{
    public static readonly Expression<Func<WorkflowDefinition, WorkflowDefinitionSummary>> WorkflowDefinitionSummary = 
        workflowDefinition => new WorkflowDefinitionSummary
        {
            Id = workflowDefinition.Id,
            DefinitionId = workflowDefinition.DefinitionId,
            Name = workflowDefinition.Name,
            Description = workflowDefinition.Description,
            Version = workflowDefinition.Version,
            IsLatest = workflowDefinition.IsLatest,
            IsPublished = workflowDefinition.IsPublished,
            MaterializerName = workflowDefinition.MaterializerName,
            CreatedAt = workflowDefinition.CreatedAt
        };
    
    public static readonly Expression<Func<WorkflowInstance, WorkflowInstanceSummary>> WorkflowInstanceSummary = 
        workflowInstance => new WorkflowInstanceSummary
        {
            Id = workflowInstance.Id,
            DefinitionId = workflowInstance.DefinitionId,
            DefinitionVersionId = workflowInstance.DefinitionVersionId,
            Version = workflowInstance.Version,
            Status = workflowInstance.Status,
            SubStatus = workflowInstance.SubStatus,
            CorrelationId = workflowInstance.CorrelationId,
            Name = workflowInstance.Name,
            CreatedAt = workflowInstance.CreatedAt,
            UpdatedAt = workflowInstance.UpdatedAt,
            FinishedAt = workflowInstance.FinishedAt
        };
}