using System;
using System.Collections.Generic;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [ActivityDefinition(
        Category = "Control Flow",
        Description = "Switch execution based on a given expression.",
        Icon = "far fa-list-alt",
        RuntimeDescription = "x => !!x.state.expression ? `Switch execution based on <strong>${ x.state.expression.expression }</strong>.` : x.definition.description",
        Outcomes = new[] { "x => x.state.cases.map(c => c.toString())" }
    )]
    public class Switch : Activity
    {
        public const string DefaultOutcome = "Default";

        public Switch()
        {
            Cases = new HashSet<string>()
            {
                OutcomeNames.Default
            };
        }

        [ActivityProperty(Hint = "The value to switch on.")]
        public string Value { get; set; } = default!;

        private HashSet<string> _cases = default!;

        [ActivityProperty(Hint = "A comma-separated list of possible outcomes of the expression.")]
        public HashSet<string> Cases
        {
            get => _cases;
            set => _cases = new HashSet<string>(value, StringComparer.OrdinalIgnoreCase);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var result = Value;
            var outcome = ContainsCase(result) ? result : DefaultOutcome;
            return Outcome(outcome, result);
        }

        private bool ContainsCase(string @case) => Cases.Contains(@case);
    }
}