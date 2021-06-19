using System;
using Elsa.Activities.Signaling.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    /// <summary>
    /// Suspends workflow execution until the specified signal is received.
    /// </summary>
    [Trigger(
        Category = "Workflows",
        Description = "Suspend workflow execution until the specified signal is received.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SignalReceived : Activity
    {
        [ActivityInput(Hint = "The name of the signal to wait for.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Signal { get; set; } = default!;

        [ActivityInput(Hint = "The scope of the signal to wait for.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }, DefaultValue = SignalScope.Instance, Category = PropertyCategories.Advanced)]
        public SignalScope Scope { get; set; } = SignalScope.Instance;

        [ActivityOutput(Hint = "The input that was received with the signal.")]
        public object? SignalInput { get; set; }
        
        [ActivityOutput] public object? Output { get; set; }

        protected override bool OnCanExecute(ActivityExecutionContext context)
        {
            if (context.Input is Signal triggeredSignal)
                return string.Equals(triggeredSignal.SignalName, Signal, StringComparison.OrdinalIgnoreCase);

            return false;
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? OnResume(context) : Suspend();

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            var triggeredSignal = context.GetInput<Signal>()!;
            SignalInput = triggeredSignal.Input;
            Output = triggeredSignal.Input;
            return Done();
        }
    }
}