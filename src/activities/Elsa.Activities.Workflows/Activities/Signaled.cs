using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Workflows.Activities
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

        protected override async Task<bool> OnCanExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var signal = await context.EvaluateAsync(Signal, cancellationToken);
            return context.Input.GetValue<string>() == signal;
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            return Halt(true);
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            return Done();
        }
    }
}