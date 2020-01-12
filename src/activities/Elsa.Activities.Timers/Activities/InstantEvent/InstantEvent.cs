using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
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

        public InstantEvent(IClock clock) => this.clock = clock;

        /// <summary>
        /// An expression that evaluates to an <see cref="NodaTime.Instant"/>
        /// </summary>
        [ActivityProperty(Hint = "An expression that evaluates to a NodaTime Instant")]
        public IWorkflowExpression<Instant> Instant
        {
            get => GetState<IWorkflowExpression<Instant>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var isExpired = await IsExpiredAsync(context, cancellationToken);

            return isExpired ? (IActivityExecutionResult)Done() : Suspend();
        }

        protected override async Task<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var isExpired = await IsExpiredAsync(context, cancellationToken);

            return isExpired ? (IActivityExecutionResult)Done() : Suspend();
        }

        private async Task<bool> IsExpiredAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var instant = await context.EvaluateAsync(Instant, cancellationToken);
            var now = clock.GetCurrentInstant();

            return now >= instant;
        }
    }
}