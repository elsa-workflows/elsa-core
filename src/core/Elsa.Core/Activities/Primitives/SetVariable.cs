using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

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

        public SetVariable(string name, IWorkflowExpression value)
        {
            VariableName = name;
            Value = value;
        }
        
        [ActivityProperty(Hint = "The name of the variable to store the value into.")]
        public string VariableName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "An expression that evaluates to the value to store in the variable.")]
        public IWorkflowExpression Value
        {
            get => GetState<IWorkflowExpression>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var value = await workflowExecutionContext.EvaluateAsync(
                Value,
                activityExecutionContext,
                cancellationToken
            );

            return new SetVariableResult(VariableName, value);
        }
    }
}