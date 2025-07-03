using System.Linq.Expressions;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.MongoDb.Helpers;

internal static class ExpressionHelpers
{
    public static readonly Expression<Func<WorkflowDefinition, WorkflowDefinitionSummary>> WorkflowDefinitionSummary =
        workflowDefinition => new()
        {
            Id = workflowDefinition.Id,
            DefinitionId = workflowDefinition.DefinitionId,
            Name = workflowDefinition.Name,
            Description = workflowDefinition.Description,
            Version = workflowDefinition.Version,
            IsLatest = workflowDefinition.IsLatest,
            IsPublished = workflowDefinition.IsPublished,
            MaterializerName = workflowDefinition.MaterializerName,
            CreatedAt = workflowDefinition.CreatedAt,
            IsReadonly = workflowDefinition.IsReadonly,
            ProviderName = workflowDefinition.ProviderName,
            ToolVersion = workflowDefinition.ToolVersion
        };

    public static readonly Expression<Func<WorkflowInstance, WorkflowInstanceSummary>> WorkflowInstanceSummary =
        workflowInstance => new()
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
            FinishedAt = workflowInstance.FinishedAt,
            IncidentCount = workflowInstance.IncidentCount,
        };

    public static readonly Expression<Func<WorkflowInstance, WorkflowInstanceId>> WorkflowInstanceId = workflowInstance => new()
    {
        Id = workflowInstance.Id
    };
    
    public static readonly Expression<Func<ActivityExecutionRecord, ActivityExecutionRecordSummary>> ActivityExecutionRecordSummary =
        workflowInstance => new()
        {
            Id = workflowInstance.Id,
            Status = workflowInstance.Status,
            ActivityId = workflowInstance.ActivityId,
            ActivityNodeId = workflowInstance.ActivityNodeId,
            ActivityType = workflowInstance.ActivityType,
            ActivityTypeVersion = workflowInstance.ActivityTypeVersion,
            ActivityName = workflowInstance.ActivityName,
            StartedAt = workflowInstance.StartedAt,
            HasBookmarks = workflowInstance.HasBookmarks,
            CompletedAt = workflowInstance.CompletedAt,
            AggregateFaultCount = workflowInstance.AggregateFaultCount,
            Metadata = workflowInstance.Metadata,
            WorkflowInstanceId = workflowInstance.WorkflowInstanceId,
            TenantId = workflowInstance.TenantId,
        };
}