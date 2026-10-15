using Elsa.AI.Abstractions.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;

namespace Elsa.AI.Host.Services;

public class RuntimeGroundingMapper(AIGroundingResultFormatter formatter)
{
    public RuntimeInstanceGroundingSummary Map(WorkflowInstanceSummary summary) =>
        new()
        {
            Id = summary.Id,
            TenantId = summary.TenantId,
            DefinitionId = summary.DefinitionId,
            DefinitionVersionId = summary.DefinitionVersionId,
            Version = summary.Version,
            Status = summary.Status.ToString(),
            SubStatus = summary.SubStatus.ToString(),
            CorrelationId = summary.CorrelationId,
            Name = summary.Name,
            IncidentCount = summary.IncidentCount,
            CreatedAt = summary.CreatedAt,
            UpdatedAt = summary.UpdatedAt,
            FinishedAt = summary.FinishedAt
        };

    public RuntimeInstanceGroundingSummary Map(WorkflowInstance instance) =>
        new()
        {
            Id = instance.Id,
            TenantId = instance.TenantId,
            DefinitionId = instance.DefinitionId,
            DefinitionVersionId = instance.DefinitionVersionId,
            Version = instance.Version,
            Status = instance.Status.ToString(),
            SubStatus = instance.SubStatus.ToString(),
            CorrelationId = instance.CorrelationId,
            Name = instance.Name,
            IncidentCount = instance.IncidentCount,
            CreatedAt = instance.CreatedAt,
            UpdatedAt = instance.UpdatedAt,
            FinishedAt = instance.FinishedAt
        };

    public IncidentGroundingSummary MapIncident(string workflowInstanceId, ActivityIncident incident) =>
        new()
        {
            WorkflowInstanceId = workflowInstanceId,
            ActivityId = incident.ActivityId,
            ActivityNodeId = incident.ActivityNodeId,
            ActivityType = incident.ActivityType,
            Message = incident.Message,
            ExceptionType = incident.Exception?.Type?.FullName,
            ExceptionMessage = incident.Exception?.Message,
            Timestamp = incident.Timestamp
        };

    public JsonObject MapState(WorkflowInstance instance) =>
        formatter.RedactObject(new JsonObject
        {
            ["id"] = instance.Id,
            ["status"] = instance.Status.ToString(),
            ["subStatus"] = instance.SubStatus.ToString(),
            ["input"] = AIGroundingJson.ToJsonObject(instance.WorkflowState.Input),
            ["output"] = AIGroundingJson.ToJsonObject(instance.WorkflowState.Output),
            ["properties"] = AIGroundingJson.ToJsonObject(instance.WorkflowState.Properties),
            ["scheduledActivityCount"] = instance.WorkflowState.ScheduledActivities.Count,
            ["bookmarkCount"] = instance.WorkflowState.Bookmarks.Count,
            ["activityExecutionContextCount"] = instance.WorkflowState.ActivityExecutionContexts.Count
        });
}
