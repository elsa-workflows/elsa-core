using System;
using Elsa.Activities.Signaling.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    /// <summary>
    /// Suspends workflow execution until the specified signal is received.
    /// </summary>
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Halt workflow execution until the specified signal is received.",
        Icon = "fas fa-traffic-light"
    )]
    public class ReceiveSignal : Activity
    {
        [ActivityProperty(Hint = "The name of the signal to wait for.")]
        public string Signal { get; set; } = default!;

        protected override bool OnCanExecute(ActivityExecutionContext context)
        {
            if (context.Input is Signal triggeredSignal)
                return string.Equals(triggeredSignal.SignalName, Signal, StringComparison.OrdinalIgnoreCase);
            
            return false;
        }

        protected override IActivityExecutionResult OnExecute() => Suspend();

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            var triggeredSignal = context.GetInput<Signal>();
            return Done(triggeredSignal.Input);
        }
    }
}