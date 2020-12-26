using System.Threading.Tasks;
using Elsa.Activities.Timers.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;

using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    [Trigger(
        Category = "Timers",
        Description = "Triggers periodically based on a specified CRON expression.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class Cron : Activity
    {
        private readonly IClock _clock;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowScheduler _workflowScheduler;
		private readonly ICrontabParser _crontabParser;

        public Cron(IWorkflowInstanceStore workflowInstanceStore, IWorkflowScheduler workflowScheduler, ICrontabParser crontabParser, IClock clock)
        {
            _clock = clock;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowScheduler = workflowScheduler;
			_crontabParser = crontabParser;
        }

        [ActivityProperty(Hint = "Specify a CRON expression. See https://crontab.guru/ for help.")]
        public string CronExpression { get; set; } = "* * * * *";

        public Instant? ExecuteAt
        {
            get => GetState<Instant?>();
            set => SetState(value);
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (context.WorkflowExecutionContext.IsFirstPass)
                return Done();

            var cancellationToken = context.CancellationToken;
            var workflowBlueprint = context.WorkflowExecutionContext.WorkflowBlueprint;
            var workflowInstance = context.WorkflowExecutionContext.WorkflowInstance;
            var executeAt = _crontabParser.GetNextOccurrence(CronExpression);

            ExecuteAt = executeAt;

            if (executeAt < _clock.GetCurrentInstant())
                return Done();

            await _workflowInstanceStore.SaveAsync(context.WorkflowExecutionContext.WorkflowInstance, cancellationToken);
            await _workflowScheduler.ScheduleWorkflowAsync(workflowBlueprint, workflowInstance.Id, Id, executeAt, cancellationToken);

            return Suspend();
        }

        protected override IActivityExecutionResult OnResume() => Done();
    }
}