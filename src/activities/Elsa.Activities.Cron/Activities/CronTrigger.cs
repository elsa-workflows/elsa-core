using System;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Cron.Activities
{
    public class CronTrigger : Activity
    {
        public WorkflowExpression<string> CronExpression { get; set; }
        public DateTime? StartTimestamp { get; set; }
    }
}