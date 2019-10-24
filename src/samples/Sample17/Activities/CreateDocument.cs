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
    public class CreateDocument : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public CreateDocument(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }
        
        public WorkflowExpression<string> TitleExpression
        {
            get => GetState<WorkflowExpression<string>>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            var title = await expressionEvaluator.EvaluateAsync(TitleExpression, context, cancellationToken);
            var document = new Document { Title = title};

            Output["Document"] = document;
            return Done();
        }
    }
}