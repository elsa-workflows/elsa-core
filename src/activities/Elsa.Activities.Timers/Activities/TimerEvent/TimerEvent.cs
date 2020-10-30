using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    [ActivityDefinition(Category = "Timers", Description = "Triggers at a specified interval.")]
    public class TimerEvent : Activity
    {
        [ActivityProperty(Hint = "An expression that evaluates to a Duration value.")]
        public Duration Timeout { get; set; } = default!;

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? (IActivityExecutionResult)Done() : Suspend();
        protected override IActivityExecutionResult OnResume() => Done();
    }
}