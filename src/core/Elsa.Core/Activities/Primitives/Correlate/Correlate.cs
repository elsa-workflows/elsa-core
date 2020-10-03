using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives
{
    /// <summary>
    /// Sets the CorrelationId of the workflow to a given value.
    /// </summary>
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Set the CorrelationId of the workflow to a given value.",
        Icon = "fas fa-link"
    )]
    public class Correlate : Activity
    {
        [ActivityProperty(Hint = "An expression that evaluates to the value to store as the correlation ID.")]
        public string Value { get; set; } = default!;

        protected override IActivityExecutionResult OnExecute() => new CorrelateResult(Value);
    }
}