namespace Elsa.Activities.Temporal.Hangfire.Models
{
    public class RunHangfireWorkflowDefinitionJobModel
    {
        public RunHangfireWorkflowDefinitionJobModel(string workflowDefinitionId, string activityId, string? cronExpression)
        {
            WorkflowDefinitionId = workflowDefinitionId;
            ActivityId = activityId;
            CronExpression = cronExpression;
        }

        public string WorkflowDefinitionId { get; set; }
        public string ActivityId { get; set; }
        public string? CronExpression { get; set; }
        public string GetIdentity() => $"{WorkflowDefinitionId}:{ActivityId}";
    }
}
