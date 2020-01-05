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
    /// <summary>
    /// Triggers at a specific instant in the future.
    /// </summary>
    [ActivityDefinition(
        Category = "Timers",
        Description = "Triggers at a specified moment in time."
    )]
    public class InstantEvent : Activity
    {
        private readonly IClock clock;

        public InstantEvent(IClock clock)
        {
            this.clock = clock;
        }

        /// <summary>
        /// An expression that evaluates to an <see cref="NodaTime.Instant"/>
        /// </summary>
        [ActivityProperty(Hint = "An expression that evaluates to a NodaTime Instant")]
        public IWorkflowExpression<Instant> Instant
        {
            get => GetState<IWorkflowExpression<Instant>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var isExpired = await IsExpiredAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);

            return isExpired ? (IActivityExecutionResult)Done() : Suspend();
        }

        protected override async Task<IActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var isExpired = await IsExpiredAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);

            return isExpired ? (IActivityExecutionResult)Done() : Suspend();
        }

        private async Task<bool> IsExpiredAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var instant = await workflowExecutionContext.EvaluateAsync(Instant, activityExecutionContext, cancellationToken);
            var now = clock.GetCurrentInstant();

            return now >= instant;
        }
    }
}