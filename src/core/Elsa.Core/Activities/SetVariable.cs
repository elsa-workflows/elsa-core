using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Scripting;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities
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
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public SetVariable(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        [ActivityProperty(Hint = "The name of the variable to store the value into.")]
        public string VariableName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "An expression that evaluates to the value to store in the variable.")]
        public IWorkflowExpression ValueScriptExpression
        {
            get => GetState<IWorkflowExpression>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var value = await expressionEvaluator.EvaluateAsync(
                ValueScriptExpression,
                workflowContext,
                cancellationToken
            );
            workflowContext.SetVariable(VariableName, value);
            return Done();
        }
    }
}