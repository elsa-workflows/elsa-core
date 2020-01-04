using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Signaling
{
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Trigger all workflows that start with or are blocked on the specified activity type.",
        Icon = "fas fa-sitemap"
    )]
    public class TriggerWorkflow : Activity
    {
        private readonly IWorkflowHost workflowHost;

        public TriggerWorkflow(IWorkflowHost workflowHost)
        {
            this.workflowHost = workflowHost;
        }

        [ActivityProperty(Hint = "An expression that evaluates to the activity type to use when triggering workflows.")]
        public IWorkflowExpression<string> ActivityType
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        [ActivityProperty(
            Hint = "An expression that evaluates to a dictionary to be provided as input when triggering workflows."
        )]
        public IWorkflowExpression<Variable> Input
        {
            get => GetState<IWorkflowExpression<Variable>>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "An expression that evaluates to the correlation ID to use when triggering workflows.")]
        public IWorkflowExpression<string> CorrelationId
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var activityType = await workflowExecutionContext.EvaluateAsync(ActivityType, activityExecutionContext, cancellationToken);
            var input = await workflowExecutionContext.EvaluateAsync(Input, activityExecutionContext, cancellationToken);
            var correlationId = await workflowExecutionContext.EvaluateAsync(CorrelationId, activityExecutionContext, cancellationToken);

            // await workflowHost.TriggerAsync(
            //     activityType,
            //     input,
            //     correlationId,
            //     cancellationToken: cancellationToken
            // );

            return Done();
        }
    }
}