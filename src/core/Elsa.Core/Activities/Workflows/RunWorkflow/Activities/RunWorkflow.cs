using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Workflows
{
    [Activity(
        Category = "Workflows",
        Description = "Runs a child workflow.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class RunWorkflow : Activity
    {
        private readonly IWorkflowRunner _workflowScheduler;
        private readonly IWorkflowRegistry _workflowRegistry;

        public RunWorkflow(IWorkflowRunner workflowScheduler, IWorkflowRegistry workflowRegistry)
        {
            _workflowScheduler = workflowScheduler;
            _workflowRegistry = workflowRegistry;
        }

        [ActivityProperty] public string WorkflowDefinitionId { get; set; } = default!;
        [ActivityProperty] public string? TenantId { get; set; } = default!;
        [ActivityProperty] public object? Input { get; set; }
        [ActivityProperty] public string? CorrelationId { get; set; }
        [ActivityProperty] public string? ContextId { get; set; }
        [ActivityProperty] public RunWorkflowMode Mode { get; set; }

        public string ChildWorkflowInstanceId
        {
            get => GetState<string>();
            set => SetState(value);
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var workflowBlueprint = (await _workflowRegistry.GetWorkflowAsync(WorkflowDefinitionId, VersionOptions.Published, cancellationToken))!;
            var workflowInstance = await _workflowScheduler.RunWorkflowAsync(workflowBlueprint, TenantId, Input, CorrelationId, ContextId, cancellationToken);
            ChildWorkflowInstanceId = workflowInstance.WorkflowInstanceId;

            return Mode switch
            {
                RunWorkflowMode.FireAndForget => Done(),
                RunWorkflowMode.Blocking => Suspend(),
                _ => Suspend()
            };
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            var input = (FinishedWorkflowModel) context.WorkflowExecutionContext.Input!;
            return Done(input);
        }

        public enum RunWorkflowMode
        {
            /// <summary>
            /// Run the specified workflow and continue with the current one. 
            /// </summary>
            FireAndForget,

            /// <summary>
            /// Run the specified workflow and continue once the child workflow finishes. 
            /// </summary>
            Blocking
        }
    }
}