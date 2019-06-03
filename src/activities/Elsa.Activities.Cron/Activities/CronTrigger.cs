using System;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Cron.Activities
{
    [DisplayName("Cron Trigger")]
    [Category("Triggers")]
    [Description("Triggers at specified intervals using CRON expressions.")]
    [IsTrigger]
    [DefaultEndpoint]
    public class CronTrigger : Activity
    {
        public WorkflowExpression<string> CronExpression { get; set; }
        public DateTime? StartTimestamp { get; set; }
    }
}