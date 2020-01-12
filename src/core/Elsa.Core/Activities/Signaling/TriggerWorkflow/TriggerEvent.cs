using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Trigger all workflows that start with or are blocked on the specified activity type.",
        Icon = "fas fa-sitemap"
    )]
    public class TriggerEvent : Activity
    {
        private readonly IWorkflowHost workflowHost;

        public TriggerEvent(IWorkflowHost workflowHost)
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
        public IWorkflowExpression Input
        {
            get => GetState<IWorkflowExpression>();
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

            await workflowHost.TriggerAsync(
                activityType,
                input,
                correlationId,
                cancellationToken: cancellationToken
            );

            return Done();
        }
    }
}