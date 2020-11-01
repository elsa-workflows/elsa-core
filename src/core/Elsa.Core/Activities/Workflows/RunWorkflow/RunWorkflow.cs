using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using Open.Linq.AsyncExtensions;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Workflows
{
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Runs a child workflow."
    )]
    public class RunWorkflow : Activity
    {
        private readonly IWorkflowScheduler _workflowScheduler;

        public RunWorkflow(IWorkflowScheduler workflowScheduler)
        {
            _workflowScheduler = workflowScheduler;
        }

        [ActivityProperty] public string WorkflowDefinitionId { get; set; } = default!;
        [ActivityProperty] public object? Input { get; set; }
        [ActivityProperty] public string? CorrelationId { get; set; }
        [ActivityProperty] public string? ContextId { get; set; }
        [ActivityProperty] public RunWorkflowMode Mode { get; set; }

        public ICollection<string> WorkflowInstanceIds
        {
            get => GetState<ICollection<string>>();
            set => SetState(value);
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var workflowInstances = await _workflowScheduler.ScheduleWorkflowDefinitionAsync(WorkflowDefinitionId, Input, CorrelationId, ContextId, cancellationToken).ToList();
            WorkflowInstanceIds = workflowInstances.Select(x => x.WorkflowInstanceId).ToList();

            return Mode switch
            {
                RunWorkflowMode.FireAndForget => Done(),
                RunWorkflowMode.Blocking => Suspend(),
                _ => Suspend()
            };
        }

        protected override ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            return base.OnResumeAsync(context, cancellationToken);
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