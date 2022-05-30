using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Exceptions;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Services.WorkflowStorage;

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
        private readonly IWorkflowStorageService _workflowStorageService;

        public InterruptTrigger(IWorkflowTriggerInterruptor workflowTriggerInterruptor, IWorkflowInstanceStore workflowInstanceStore, IWorkflowStorageService workflowStorageService)
        {
            _workflowTriggerInterruptor = workflowTriggerInterruptor;
            _workflowInstanceManager = workflowInstanceStore;
            _workflowStorageService = workflowStorageService;
        }

        [ActivityInput(Hint = "The ID of the workflow instance to resume.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string WorkflowInstanceId { get; set; } = default!;

        [ActivityInput(Hint = "The ID of the blocking activity to trigger.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string BlockingActivityId { get; set; } = default!;

        [ActivityInput(Hint = "An optional input to pass to the blocking activity.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public object? Input { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var workflowInstance = await _workflowInstanceManager.FindByIdAsync(WorkflowInstanceId, cancellationToken);

            if (workflowInstance == null)
                throw new WorkflowException($"Could not find workflow instance by ID {WorkflowInstanceId}");

            if (Input != null)
                workflowInstance.Input = await _workflowStorageService.SaveAsync(new WorkflowInput(Input), workflowInstance, cancellationToken);

            await _workflowTriggerInterruptor.InterruptActivityAsync(workflowInstance!, BlockingActivityId, cancellationToken);
            return Done();
        }
    }
}