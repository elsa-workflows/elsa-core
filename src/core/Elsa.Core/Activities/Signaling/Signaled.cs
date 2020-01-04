using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Signaling
{
    /// <summary>
    /// Halts workflow execution until the specified signal is received.
    /// </summary>
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Halt workflow execution until the specified signal is received.",
        Icon = "fas fa-traffic-light"
    )]
    public class Signaled : Activity
    {
        [ActivityProperty(Hint = "An expression that evaluates to the name of the signal to wait for.")]
        public IWorkflowExpression<string> Signal
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        protected override async Task<bool> OnCanExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var signal = await workflowExecutionContext.EvaluateAsync(Signal, activityExecutionContext, cancellationToken);
            return activityExecutionContext.Input.GetValue<string>() == signal;
        }

        protected override IActivityExecutionResult OnExecute(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext)
        {
            return Suspend();
        }

        protected override IActivityExecutionResult OnResume(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext context)
        {
            return Done();
        }
    }
}