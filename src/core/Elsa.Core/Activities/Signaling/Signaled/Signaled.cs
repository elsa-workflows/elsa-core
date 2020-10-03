using System;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    /// <summary>
    /// Halts workflow execution until the specified signal is received.
    /// </summary>
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Halt workflow execution until the specified signal is received.",
        Icon = "fas fa-traffic-light"
    )]
    public class Signaled : Activity
    {
        [ActivityProperty(Hint = "An expression that evaluates to the name of the signal to wait for.")]
        public string Signal { get; set; } = default!;

        protected override bool OnCanExecute(ActivityExecutionContext context)
        {
            var signal = Signal;
            var triggeredSignal = (TriggeredSignal)context.Input!;
            return string.Equals(triggeredSignal.SignalName, signal, StringComparison.OrdinalIgnoreCase);
        }

        protected override IActivityExecutionResult OnExecute() => Suspend();
        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            var triggeredSignal = (TriggeredSignal)context.Input!;
            return Done(triggeredSignal.Input);
        }
    }
}