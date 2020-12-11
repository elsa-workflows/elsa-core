using System.Threading.Tasks;
using Elsa.Activities.Timers.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Repositories;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    /// <summary>
    /// Triggers at a specific instant in the future.
    /// </summary>
    [Trigger(
        Category = "Timers",
        Description = "Triggers at a specified moment in time."
    )]
    public class StartAt : Activity
    {
        private readonly IWorkflowInstanceRepository _workflowInstanceManager;
        private readonly IWorkflowScheduler _workflowScheduler;

        public StartAt(IWorkflowInstanceRepository workflowInstanceRepository, IWorkflowScheduler workflowScheduler)
        {
            _workflowInstanceManager = workflowInstanceRepository;
            _workflowScheduler = workflowScheduler;
        }

        [ActivityProperty(Hint = "An instant in the future at which this activity should execute.")]
        public Instant Instant { get; set; }

        public Instant? ExecuteAt
        {
            get => GetState<Instant?>();
            set => SetState(value);
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (context.WorkflowExecutionContext.IsFirstPass)
                return Done();

            var cancellationToken = context.CancellationToken;
            var workflowBlueprint = context.WorkflowExecutionContext.WorkflowBlueprint;
            var workflowInstance = context.WorkflowExecutionContext.WorkflowInstance;
            var executeAt = Instant;

            ExecuteAt = executeAt;

            await _workflowInstanceManager.SaveAsync(context.WorkflowExecutionContext.WorkflowInstance, cancellationToken);
            await _workflowScheduler.ScheduleWorkflowAsync(workflowBlueprint, workflowInstance.WorkflowInstanceId, Id, executeAt, cancellationToken);

            return Suspend();
        }

        protected override IActivityExecutionResult OnResume() => Done();
    }
}