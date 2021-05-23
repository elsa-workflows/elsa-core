using System;
using System.Security.Cryptography;
using System.Text;
using Elsa.Activities.Temporal.Common.Options;

namespace Elsa.Activities.Temporal.Hangfire.Models
{
    public class RunHangfireWorkflowJobModel
    {
        public RunHangfireWorkflowJobModel(string? workflowDefinitionId, string activityId, string? workflowInstanceId, string? tenantId, string? cronExpression, ClusterMode? clusterMode)
        {
            WorkflowDefinitionId = workflowDefinitionId;
            WorkflowInstanceId = workflowInstanceId;
            ActivityId = activityId;
            TenantId = tenantId;
            CronExpression = cronExpression;
            ClusterMode = clusterMode;
        }

        public string? WorkflowDefinitionId { get; set; }
        public string? WorkflowInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string? TenantId { get; set; }
        public string? CronExpression { get; set; }
        public ClusterMode? ClusterMode { get; }
        public bool IsRecurringJob => string.IsNullOrEmpty(CronExpression) == false;
        
        public string GetIdentity()
        {
            var text = $"{TenantId ?? "default"}:{WorkflowInstanceId ?? WorkflowDefinitionId}:{ActivityId}";
            var bytes = Encoding.UTF8.GetBytes(text);

            using var sha1 = new SHA1Managed();
            var hash = sha1.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
