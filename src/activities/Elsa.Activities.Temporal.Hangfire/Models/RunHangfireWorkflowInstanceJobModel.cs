namespace Elsa.Activities.Temporal.Hangfire.Models
{
    public class RunHangfireWorkflowInstanceJobModel
    {
        public RunHangfireWorkflowInstanceJobModel(string workflowInstanceId, string activityId, string? cronExpression)
        {
            WorkflowInstanceId = workflowInstanceId;
            ActivityId = activityId;
            CronExpression = cronExpression;
        }

        public string WorkflowInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string? CronExpression { get; set; }
        public string GetIdentity() => $"{WorkflowInstanceId}:{ActivityId}";
    }
}