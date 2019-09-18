﻿using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Primitives
{
    public class SetVariable : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public SetVariable(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        public string VariableName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        public WorkflowExpression ValueExpression
        {
            get => GetState<WorkflowExpression>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var value = await expressionEvaluator.EvaluateAsync(ValueExpression, ValueExpression.Type, workflowContext, cancellationToken);
            workflowContext.CurrentScope.SetVariable(VariableName, value);
            return Done();
        }
    }
}