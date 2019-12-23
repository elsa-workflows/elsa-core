using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Scripting;
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
        /// An expression that evaluates to an <see cref="Instant"/>
        /// </summary>
        [ActivityProperty(Hint = "An expression that evaluates to a NodaTime Instant")]
        public IWorkflowExpression<Instant> InstantScriptExpression
        {
            get => GetState<IWorkflowExpression<Instant>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var isExpired = await IsExpiredAsync(context, cancellationToken);

            return isExpired ? (IActivityExecutionResult)Done() : Halt();
        }

        protected override async Task<IActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var isExpired = await IsExpiredAsync(workflowContext, cancellationToken);

            return isExpired ? (IActivityExecutionResult)Done() : Halt();
        }

        private async Task<bool> IsExpiredAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var instant = await workflowContext.EvaluateAsync(InstantScriptExpression, cancellationToken);
            var now = clock.GetCurrentInstant();

            return now >= instant;
        }
    }
}