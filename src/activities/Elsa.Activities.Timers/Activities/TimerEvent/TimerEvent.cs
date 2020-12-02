using System.Threading.Tasks;
using Elsa.Activities.Timers.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    [Trigger(Category = "Timers", Description = "Triggers at a specified interval.")]
    public class TimerEvent : Activity
    {
        private readonly IWorkflowInstanceManager _workflowInstanceManager;
        private readonly IWorkflowScheduler _workflowScheduler;
        private readonly IClock _clock;

        public TimerEvent(IWorkflowInstanceManager workflowInstanceManager, IWorkflowScheduler workflowScheduler, IClock clock)
        {
            _workflowInstanceManager = workflowInstanceManager;
            _workflowScheduler = workflowScheduler;
            _clock = clock;
        }

        [ActivityProperty(Hint = "The time interval at which this activity should tick.")]
        public Duration Timeout { get; set; } = default!;

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
            var executeAt = _clock.GetCurrentInstant().Plus(Timeout);
            
            ExecuteAt = executeAt;

            await _workflowInstanceManager.SaveAsync(context.WorkflowExecutionContext.WorkflowInstance, cancellationToken);
            await _workflowScheduler.ScheduleWorkflowAsync(workflowBlueprint, workflowInstance.WorkflowInstanceId, Id, executeAt, cancellationToken);

            return Suspend();
        }

        protected override IActivityExecutionResult OnResume() => Done();
    }
}