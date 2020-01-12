using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Removes any blocking activities and sets the status of the workflow to Completed.",
        Icon = "fas fa-flag-checkered"
    )]
    public class Complete : Activity
    {
        [ActivityProperty(Hint = "An expression that evaluates to the activity's output.'")]
        public IWorkflowExpression OutputValue
        {
            get => GetState<IWorkflowExpression>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var output = await context.EvaluateAsync(OutputValue, cancellationToken) ?? new Variable();
            
            context.WorkflowExecutionContext.BlockingActivities.Clear();
            context.WorkflowExecutionContext.Complete();
            
            return Done(Variable.From(output));
        }
    }
}