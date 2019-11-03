using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
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

        public WorkflowExpression<string> TitleExpression
        {
            get => GetState<WorkflowExpression<string>>();
            set => SetState(value);
        }

        public WorkflowExpression<int> AgeExpression
        {
            get => GetState<WorkflowExpression<int>>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            var name = await expressionEvaluator.EvaluateAsync(TitleExpression, context, cancellationToken);
            var age = await expressionEvaluator.EvaluateAsync(AgeExpression, context, cancellationToken);
            var person = new Person { FullName = name, Age = age };

            Output.SetVariable("Person", person);
            Output.SetVariable("FullName", person.FullName);
            Output.SetVariable("Age", person.Age);
            
            return Done();
        }
    }
}