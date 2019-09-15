using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow
{
    public class ForEach : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public ForEach(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        public WorkflowExpression<IList<object>> CollectionExpression
        {
            get => GetState(() => new JavaScriptExpression<IList<object>>("[]"));
            set => SetState(value);
        }
        
        public string IteratorName
        {
            get => GetState(() => "CurrentValue");
            set => SetState(value);
        }

        public int CurrentIndex
        {
            get => GetState(() => 0);
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var collection = await expressionEvaluator.EvaluateAsync(CollectionExpression, context, cancellationToken);
            var index = CurrentIndex;

            if (index >= collection.Count)
            {
                context.EndScope();
                CurrentIndex = 0;
                return Done();
            }

            var value = collection[index];
            CurrentIndex++;

            if (index == 0)
            {
                context.BeginScope();
            }
            
            context.CurrentScope.SetVariable(IteratorName, value);

            return Outcome(OutcomeNames.Iterate);
        }
    }
}