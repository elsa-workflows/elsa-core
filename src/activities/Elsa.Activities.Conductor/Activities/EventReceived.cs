using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Conductor.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Conductor
{
    [Trigger(
        Category = "Conductor",
        Description = "Waits for an event sent from your application."
    )]
    public class EventReceived : Activity
    {
        [ActivityInput(
            Label = "Event",
            Hint = "The event to wait for.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string EventName { get; set; } = default!;

        [ActivityInput(
            Hint = "Enter one or more possible outcomes for this event.",
            UIHint = ActivityInputUIHints.MultiText,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json }
        )]
        public ISet<string> OutcomeNames { get; set; } = new HashSet<string>();

        [ActivityOutput(Hint = "Any input that was sent along with the event from your application.")]
        public object? Payload { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.IsFirstPass ? OnExecuteInternal(context) : Suspend();
        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => OnExecuteInternal(context);

        private IActivityExecutionResult OnExecuteInternal(ActivityExecutionContext context)
        {
            var eventModel = context.GetInput<EventModel>()!;
            var outcomes = eventModel.Outcomes;

            if (outcomes?.Any() == false)
                outcomes = new[] { Elsa.OutcomeNames.Done };

            Payload = eventModel.Payload;
            return base.Outcomes(outcomes!);
        }
    }
}