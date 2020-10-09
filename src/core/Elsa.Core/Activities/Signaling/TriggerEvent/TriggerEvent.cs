using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
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
        private readonly IWorkflowScheduler _workflowScheduler;

        public TriggerEvent(IWorkflowScheduler workflowScheduler)
        {
            this._workflowScheduler = workflowScheduler;
        }

        [ActivityProperty(Hint = "An expression that evaluates to the activity type to use when triggering workflows.")]
        public string ActivityType { get; set; } = default!;

        [ActivityProperty(
            Hint = "An expression that evaluates to a dictionary to be provided as input when triggering workflows."
        )]
        public object? Input { get; set; }

        [ActivityProperty(Hint = "An expression that evaluates to the correlation ID to use when triggering workflows.")]
        public string? CorrelationId { get; set; }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            await _workflowScheduler.TriggerWorkflowsAsync(
                ActivityType,
                Input,
                CorrelationId,
                cancellationToken: cancellationToken
            );

            return Done();
        }
    }
}