using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives
{
    [Activity(
        DisplayName = "Set Transient Variable",
        Description = "Set a transient variable on the current workflow execution context.",
        Category = "Primitives",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SetTransientVariable : Activity
    {
        [ActivityInput(Hint = "The name of the transient variable to store the value into.")]
        public string VariableName { get; set; } = default!;

        [ActivityInput(Hint = "The value to store in the transient variable.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public object? Value { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            context.SetTransientVariable(VariableName, Value);
            return Done();
        }
    }
}