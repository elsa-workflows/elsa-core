using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives
{
    [ActivityDefinition(
        DisplayName = "Set Variable",
        Description = "Set variable on the workflow.",
        Category = "Primitives",
        RuntimeDescription = "x => !!x.state.variableName ? `<strong>${x.state.variableName}</strong> = <strong>${x.state.valueExpression.expression}</strong><br/>${x.state.valueExpression.syntax}` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SetVariable : Activity
    {
        public SetVariable()
        {
        }

        public SetVariable(string name, object? value)
        {
            VariableName = name;
            Value = value;
        }
        
        [ActivityProperty(Hint = "The name of the variable to store the value into.")]
        public string VariableName { get; set; }

        [ActivityProperty(Hint = "An expression that evaluates to the value to store in the variable.")]
        public object? Value { get; set; }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            context.SetVariable(VariableName, Value);
            return Done();
        }
    }
}