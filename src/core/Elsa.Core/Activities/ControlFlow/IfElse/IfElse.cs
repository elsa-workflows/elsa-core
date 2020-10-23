using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [ActivityDefinition(
        DisplayName = "If/Else",
        Category = "Control Flow",
        Description = "Evaluate a Boolean expression and continue execution depending on the result.",
        RuntimeDescription = "x => !!x.state.expression ? `Evaluate <strong>${ x.state.expression.expression }</strong> and continue execution depending on the result.` : x.definition.description",
        Outcomes = new[] { True, False, OutcomeNames.Done }
    )]
    public class IfElse : Activity
    {
        public const string True = "True";
        public const string False = "False";
        
        [ActivityProperty(Hint = "The condition to evaluate.")]
        public bool Condition { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var outcome = Condition ? True : False;
            return Outcomes(OutcomeNames.Done, outcome);
        }
    }
}