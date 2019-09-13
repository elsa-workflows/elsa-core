using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Activities.Timers.Activities
{
    /// <summary>
    /// Triggers at a specific instant in the future.
    /// </summary>
    public class InstantEvent : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly IClock clock;

        public InstantEvent(IWorkflowExpressionEvaluator expressionEvaluator, IClock clock)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.clock = clock;
        }

        /// <summary>
        /// An expression that evaluates to an <see cref="Instant"/>
        /// </summary>
        public WorkflowExpression<Instant> InstantExpression
        {
            get => GetState<WorkflowExpression<Instant>>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var isExpired = await IsExpiredAsync(context, cancellationToken);

            return isExpired ? Done() : Halt();
        }

        protected override async Task<ActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var isExpired = await IsExpiredAsync(workflowContext, cancellationToken);

            return isExpired ? Done() : Halt();
        }

        private async Task<bool> IsExpiredAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var instant = await expressionEvaluator.EvaluateAsync(InstantExpression, workflowContext, cancellationToken);
            var now = clock.GetCurrentInstant();

            return now >= instant;
        }
    }
}