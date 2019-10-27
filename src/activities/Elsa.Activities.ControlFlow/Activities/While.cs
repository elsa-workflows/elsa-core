using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Scripting.JavaScript;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow.Activities
{
    [ActivityDefinition(Category = "Control Flow", Description = "Execute while a given condition is true.", Icon = "far fa-circle")]
    public class While : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public While(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        [ActivityProperty(Hint = "Enter an expression that evaluates to a boolean value.")]
        public WorkflowExpression<bool> ConditionExpression
        {
            get => GetState(() => new JavaScriptExpression<bool>("true"));
            set => SetState(value);
        }
        
        public bool HasStarted
        {
            get => GetState(() => false);
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            var loop = await expressionEvaluator.EvaluateAsync(ConditionExpression, context, cancellationToken);

            if(HasStarted)
                context.EndScope();
            
            if (loop)
            {
                if (!HasStarted) 
                    HasStarted = true;

                context.BeginScope();
                return Outcome(OutcomeNames.Iterate);
            }

            return Done();
        }
    }
}