using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives
{
    /// <summary>
    /// Sets the CorrelationId of the workflow to a given value.
    /// </summary>
    [Activity(
        Category = "Workflows",
        Description = "Set the CorrelationId of the workflow to a given value.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class Correlate : Activity
    {
        [ActivityInput(Hint = "An expression that evaluates to the value to store as the correlation ID.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Value { get; set; } = default!;
        
        [ActivityOutput] public string? Output { get; set; }

        protected override IActivityExecutionResult OnExecute()
        {
            Output = Value;
            return Combine(Done(), new CorrelateResult(Value));
        }
    }
}