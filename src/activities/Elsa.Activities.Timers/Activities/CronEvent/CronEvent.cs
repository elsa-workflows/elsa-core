using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    [Trigger(
        Category = "Timers",
        Description = "Triggers periodically based on a specified CRON expression.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class CronEvent : Activity
    {
        [ActivityProperty(Hint = "Specify a CRON expression. See https://crontab.guru/ for help.")]
        public string CronExpression { get; set; } = "* * * * *";

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? (IActivityExecutionResult) Done() : Suspend();
        protected override IActivityExecutionResult OnResume() => Done();
    }
}