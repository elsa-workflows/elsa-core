using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Results;
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
        Outcomes = "x => x.state.cases.map(c => c.toString())"
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
        public IWorkflowExpression<string> Value
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "A comma-separated list of possible outcomes of the expression.")]
        public HashSet<string> Cases
        {
            get => GetState<HashSet<string>>();
            set => SetState(new HashSet<string>(value, StringComparer.OrdinalIgnoreCase));
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var result = await context.EvaluateAsync(Value, cancellationToken);

            if (ContainsCase(result) || !ContainsCase(OutcomeNames.Default))
                return Done(result, Variable.From(result));

            return Done(OutcomeNames.Default, Variable.From(result));
        }

        private bool ContainsCase(string @case) => Cases.Contains(@case);
    }
}