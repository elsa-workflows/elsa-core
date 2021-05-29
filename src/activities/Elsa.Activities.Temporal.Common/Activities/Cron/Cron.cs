using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Temporal
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
        private readonly IWorkflowInstanceScheduler _workflowScheduler;
        private readonly ICrontabParser _crontabParser;

        public Cron(IWorkflowInstanceStore workflowInstanceStore, IWorkflowInstanceScheduler workflowScheduler, ICrontabParser crontabParser, IClock clock)
        {
            _clock = clock;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowScheduler = workflowScheduler;
            _crontabParser = crontabParser;
        }

        [ActivityInput(Hint = "Specify a CRON expression.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
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
            var workflowInstance = context.WorkflowExecutionContext.WorkflowInstance;
            var executeAt = _crontabParser.GetNextOccurrence(CronExpression);

            ExecuteAt = executeAt;

            if (executeAt < _clock.GetCurrentInstant())
                return Done();

            await _workflowInstanceStore.SaveAsync(context.WorkflowExecutionContext.WorkflowInstance, cancellationToken);
            await _workflowScheduler.ScheduleAsync(workflowInstance.Id, Id, executeAt, null, cancellationToken);

            return Suspend();
        }

        protected override IActivityExecutionResult OnResume() => Done();
    }
}