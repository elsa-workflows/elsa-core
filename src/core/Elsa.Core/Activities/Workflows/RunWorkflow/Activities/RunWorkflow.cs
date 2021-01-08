using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
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

        [ActivityProperty] public string? WorkflowDefinitionId { get; set; } = default!;
        [ActivityProperty] public string? TenantId { get; set; } = default!;
        [ActivityProperty] public object? Input { get; set; }
        [ActivityProperty] public string? CorrelationId { get; set; }
        [ActivityProperty] public string? ContextId { get; set; }
        [ActivityProperty] public Variables? CustomAttributes { get; set; } = default!;
        [ActivityProperty] public RunWorkflowMode Mode { get; set; }

        public string ChildWorkflowInstanceId
        {
            get => GetState<string>()!;
            set => SetState(value);
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var workflowBlueprint = await FindWorkflowBlueprintAsync(cancellationToken);
            var workflowInstance = await _workflowScheduler.RunWorkflowAsync(workflowBlueprint!, TenantId, Input, CorrelationId, ContextId, cancellationToken);
            ChildWorkflowInstanceId = workflowInstance.Id;

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

        private async Task<IWorkflowBlueprint?> FindWorkflowBlueprintAsync(CancellationToken cancellationToken)
        {
            var query = (IEnumerable<IWorkflowBlueprint>)(await _workflowRegistry.GetWorkflowsAsync(cancellationToken).ToListAsync(cancellationToken));

            query = query.Where(x => x.WithVersion(VersionOptions.Published));

            if (WorkflowDefinitionId != null)
                query = query.Where(x => x.Id == WorkflowDefinitionId);

            if (TenantId != null)
                query = query.Where(x => x.TenantId == TenantId);

            if (CustomAttributes != null)
                foreach (var customAttribute in CustomAttributes.Data)
                    query = query.Where(x => Equals(x.CustomAttributes.Get(customAttribute.Key), customAttribute.Value));

            return query.FirstOrDefault();
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