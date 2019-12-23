using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Workflows.Activities
{
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Trigger all workflows that start with or are blocked on the specified activity type.",
        Icon = "fas fa-sitemap"
    )]
    public class TriggerWorkflow : Activity
    {
        private readonly IWorkflowRunner workflowRunner;

        public TriggerWorkflow(IWorkflowRunner workflowRunner)
        {
            this.workflowRunner = workflowRunner;
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

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var activityType = await context.EvaluateAsync(ActivityType, cancellationToken);
            var input = await context.EvaluateAsync(Input, cancellationToken);
            var correlationId = await context.EvaluateAsync(CorrelationId, cancellationToken);

            await workflowRunner.TriggerAsync(
                activityType,
                input,
                correlationId,
                cancellationToken: cancellationToken
            );

            return Done();
        }
    }
}