using System.Linq.Expressions;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Entities;

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

    public static readonly Expression<Func<WorkflowInstance, WorkflowInstanceId>> WorkflowInstanceId = workflowInstance => new WorkflowInstanceId
    {
        Id = workflowInstance.Id
    };
    
    public static readonly Expression<Func<ActivityExecutionRecord, ActivityExecutionRecordSummary>> ActivityExecutionRecordSummary =
        workflowInstance => new ActivityExecutionRecordSummary
        {
            Id = workflowInstance.Id,
            Status = workflowInstance.Status,
            ActivityId = workflowInstance.ActivityId,
            ActivityNodeId = workflowInstance.ActivityNodeId,
            ActivityType = workflowInstance.ActivityType,
            ActivityTypeVersion = workflowInstance.ActivityTypeVersion,
            ActivityName = workflowInstance.ActivityName,
            StartedAt = workflowInstance.StartedAt,
            HasBookmarks = workflowInstance.HasBookmarks
        };
}