﻿using System.Threading;
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
        Category = "Primitives"
    )]
    [ActivityDefinitionDesigner(
        Description =
            "x => !!x.state.variableName ? `${x.state.expression.syntax}: <strong>${x.state.variableName}</strong> = <strong>${x.state.expression.expression}</strong>` : x.definition.description",
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
        public WorkflowExpression ValueExpression
        {
            get => GetState<WorkflowExpression>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext workflowContext,
            CancellationToken cancellationToken)
        {
            var value = await expressionEvaluator.EvaluateAsync(
                ValueExpression,
                ValueExpression.Type,
                workflowContext,
                cancellationToken
            );
            workflowContext.CurrentScope.SetVariable(VariableName, value);
            return Done();
        }
    }
}