using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Sample17.Models;

namespace Sample17.Activities
{
    public class CreatePerson : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public CreatePerson(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        public IWorkflowExpression<string> TitleScriptExpression
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        public IWorkflowExpression<int> AgeScriptExpression
        {
            get => GetState<IWorkflowExpression<int>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(
            ActivityExecutionContext context,
            CancellationToken cancellationToken)
        {
            var name = await expressionEvaluator.EvaluateAsync(TitleScriptExpression, context, cancellationToken);
            var age = await expressionEvaluator.EvaluateAsync(AgeScriptExpression, context, cancellationToken);
            var person = new Person { FullName = name, Age = age };

            return Done(person);
        }
    }
}