using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(
        DisplayName = "If/Else",
        Category = "Control Flow",
        Description = "Evaluate a Boolean expression and continue execution depending on the result.",
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