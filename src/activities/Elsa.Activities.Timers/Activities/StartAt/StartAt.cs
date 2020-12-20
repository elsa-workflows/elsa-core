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
    /// <summary>
    /// Triggers at a specific instant in the future.
    /// </summary>
    [Trigger(
        Category = "Timers",
        Description = "Triggers at a specified moment in time."
    )]
    public class StartAt : Activity
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowScheduler _workflowScheduler;
        private readonly IClock _clock;

        public StartAt(IWorkflowInstanceStore workflowInstanceStore, IWorkflowScheduler workflowScheduler, IClock clock)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _workflowScheduler = workflowScheduler;
            _clock = clock;
        }

        [ActivityProperty(Hint = "An instant in the future at which this activity should execute.")]
        public Instant Instant { get; set; }

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
            var executeAt = Instant;

            ExecuteAt = executeAt;

            if (executeAt <= _clock.GetCurrentInstant())
                return Done();

            await _workflowInstanceStore.SaveAsync(context.WorkflowExecutionContext.WorkflowInstance, cancellationToken);
            await _workflowScheduler.ScheduleWorkflowAsync(workflowBlueprint, workflowInstance.Id, Id, executeAt, cancellationToken);

            return Suspend();
        }

        protected override IActivityExecutionResult OnResume() => Done();
    }
}