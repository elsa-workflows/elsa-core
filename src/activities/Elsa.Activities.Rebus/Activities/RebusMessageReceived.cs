using System;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Rebus
{
    [Trigger(Category = "Rebus", Description = "Triggered when a message is received.", Outcomes = new[] { OutcomeNames.Done })]
    public class RebusMessageReceived : Activity
    {
        [ActivityInput(Hint = "The type of message to receive.")]
        public Type MessageType { get; set; } = default!;

        [ActivityOutput] public object? Output { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            Output = context.Input;
            return Done();
        }
    }
}