namespace Elsa.Activities.Timers.Hangfire.Models
{
    public class RunHangfireWorkflowJobModel
    {
        public RunHangfireWorkflowJobModel(string workflowDefinitionId, string activityId, string? workflowInstanceId, string? tenantId, string? cronExpression)
        {
            WorkflowDefinitionId = workflowDefinitionId;
            WorkflowInstanceId = workflowInstanceId;
            ActivityId = activityId;
            TenantId = tenantId;
            CronExpression = cronExpression;
        }

        public string WorkflowDefinitionId { get; set; }
        public string? WorkflowInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string? TenantId { get; set; }
        public string? CronExpression { get; set; }
        public bool IsRecurringJob => string.IsNullOrEmpty(CronExpression) == false;
        
        public string GetIdentity() => $"Elsa-tenant:{TenantId ?? "default"}-workflow-instance:{WorkflowInstanceId ?? WorkflowDefinitionId}-activity:{ActivityId}";
    }
}
