using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Activities.Timers.Activities
{
    [ActivityDefinition(
        Category = "Timers",
        Description = "Triggers at a specified interval."
    )]
    public class TimerEvent : Activity
    {
        private readonly IClock clock;

        public TimerEvent(IClock clock)
        {
            this.clock = clock;
        }

        [ActivityProperty(Hint = "An expression that evaluates to a TimeSpan value")]
        public IWorkflowExpression<TimeSpan> Timeout
        {
            get => GetState<IWorkflowExpression<TimeSpan>>(() => new LiteralExpression<TimeSpan>("00:01:00"));
            set => SetState(value);
        }

        public Instant? StartTime
        {
            get => GetState<Instant?>();
            set => SetState(value);
        }

        protected override async Task<bool> OnCanExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            return StartTime == null || await IsExpiredAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);
        }

        protected override IActivityExecutionResult OnExecute(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext)
        {
            return Suspend();
        }

        protected override async Task<IActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            if (await IsExpiredAsync(workflowExecutionContext, activityExecutionContext, cancellationToken))
            {
                StartTime = null;
                return Done();
            }
            
            return Suspend();
        }

        private async Task<bool> IsExpiredAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var now = clock.GetCurrentInstant();

            if (StartTime == null)
                StartTime = now;
            
            var timeSpan = await workflowExecutionContext.EvaluateAsync(Timeout, activityExecutionContext, cancellationToken);
            var expiresAt = StartTime.Value.ToDateTimeUtc() + timeSpan;
            
            return now.ToDateTimeUtc() >= expiresAt;
        }
    }
}