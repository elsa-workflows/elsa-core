using System;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Cron.Activities
{
    [ActivityDisplayName("Cron Trigger")]
    [ActivityCategory("Triggers")]
    [ActivityDescription("Triggers at specified intervals using CRON expressions.")]
    [IsTrigger]
    [DefaultEndpoint]
    public class CronTrigger : Activity
    {
        public WorkflowExpression<string> CronExpression { get; set; }
        public DateTime? StartTimestamp { get; set; }
    }
}