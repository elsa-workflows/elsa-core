using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        public Switch()
        {
            Cases = new HashSet<string>()
            {
                OutcomeNames.Default
            };
        }

        [ActivityProperty(Hint = "The value to evaluate. The evaluated value will be used to switch on.")]
        public string Value { get; set; }

        private HashSet<string> _cases;
        [ActivityProperty(Hint = "A comma-separated list of possible outcomes of the expression.")]
        public HashSet<string> Cases
        {
            get => _cases;
            set => _cases = new HashSet<string>(value, StringComparer.OrdinalIgnoreCase);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var result = Value;

            if (ContainsCase(result) || !ContainsCase(OutcomeNames.Default))
                return Done(result, (object)result);

            return Done(OutcomeNames.Default, (object)result);
        }

        private bool ContainsCase(string @case) => Cases.Contains(@case);
    }
}