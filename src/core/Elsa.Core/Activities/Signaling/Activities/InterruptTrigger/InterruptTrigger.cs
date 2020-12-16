using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Extensions;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    /// <summary>
    /// Resumes workflow execution that is blocked on a given activity.
    /// </summary>
    [Trigger(
        Category = "Workflows",
        Description = "Resumes suspended workflows that are blocked on a specific trigger.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class InterruptTrigger : Activity
    {
        private readonly IWorkflowTriggerInterruptor _workflowTriggerInterruptor;
        private readonly IWorkflowInstanceStore _workflowInstanceManager;

        public InterruptTrigger(IWorkflowTriggerInterruptor workflowTriggerInterruptor, IWorkflowInstanceStore workflowInstanceStore)
        {
            _workflowTriggerInterruptor = workflowTriggerInterruptor;
            _workflowInstanceManager = workflowInstanceStore;
        }
        
        [ActivityProperty(Hint = "The ID of the workflow instance to resume.")]
        public string WorkflowInstanceId { get; set; } = default!;
        
        [ActivityProperty(Hint = "The ID of the blocking activity to trigger.")]
        public string BlockingActivityId { get; set; } = default!;
        
        [ActivityProperty(Hint = "An optional input to pass to the blocking activity.")]
        public object? Input { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var workflowInstance = await _workflowInstanceManager.FindByIdAsync(WorkflowInstanceId, context.CancellationToken);
            await _workflowTriggerInterruptor.InterruptActivityAsync(workflowInstance!, BlockingActivityId, Input, context.CancellationToken);
            return Done();
        }
    }
}