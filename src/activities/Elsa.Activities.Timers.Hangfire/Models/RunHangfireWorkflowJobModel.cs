using System;

namespace Elsa.Activities.Timers.Hangfire.Models
{
    public class RunHangfireWorkflowJobModel
    {
        public RunHangfireWorkflowJobModel(string workflowDefinitionId, string activityId, string? workflowInstanceId, string? tenantId, DateTimeOffset? dateTimeOffset)
        {
            WorkflowDefinitionId = workflowDefinitionId;
            WorkflowInstanceId = workflowInstanceId;
            ActivityId = activityId;
            TenantId = tenantId;
            DateTimeOffset = dateTimeOffset;
        }
     
        public string WorkflowDefinitionId { get; set; }
        public string? WorkflowInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string? TenantId { get; set; }
        public DateTimeOffset? DateTimeOffset { get; set; }
        public bool RecurringJob => DateTimeOffset.HasValue == false;

        public string GetIdentity() => $"Elsa-tenant:{TenantId ?? "default"}-workflow-instance:{WorkflowInstanceId ?? WorkflowDefinitionId}-activity:{ActivityId}";
    }
}
