using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Services.Workflows
{
    public class WorkflowInstanceExecutor : IWorkflowInstanceExecutor
    {
        private readonly IResumesWorkflow _workflowRunner;
        public IWorkflowInstanceStore WorkflowInstanceStore { get; }
        private readonly ILogger _logger;

        public WorkflowInstanceExecutor(IResumesWorkflow workflowRunner, IWorkflowInstanceStore workflowInstanceStore, ILogger<WorkflowInstanceExecutor> logger)
        {
            _workflowRunner = workflowRunner;
            WorkflowInstanceStore = workflowInstanceStore;
            _logger = logger;
        }

        public async Task<RunWorkflowResult> ExecuteAsync(string workflowInstanceId, string? activityId, object? input = default, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await WorkflowInstanceStore.FindByIdAsync(workflowInstanceId, cancellationToken);
            
            if (!ValidatePreconditions(workflowInstanceId, workflowInstance, activityId))
                return new RunWorkflowResult(workflowInstance, activityId, false);
            
            return await _workflowRunner.ResumeWorkflowAsync(
                workflowInstance!,
                activityId,
                input, cancellationToken);
        }

        public async Task<RunWorkflowResult> ExecuteAsync(WorkflowInstance workflowInstance, string? activityId, object? input = default, CancellationToken cancellationToken = default)
        {
            if (!ValidatePreconditions(workflowInstance.Id, workflowInstance, activityId))
                return new RunWorkflowResult(workflowInstance, activityId, false);

            return await _workflowRunner.ResumeWorkflowAsync(
                workflowInstance!,
                activityId,
                input, cancellationToken);
        }

        private bool ValidatePreconditions(string? workflowInstanceId, WorkflowInstance? workflowInstance, string? activityId)
        {
            if (workflowInstance == null)
            {
                _logger.LogWarning("Could not run workflow instance with ID {WorkflowInstanceId} because it does not exist", workflowInstanceId);
                return false;
            }

            if (workflowInstance.WorkflowStatus != WorkflowStatus.Suspended && workflowInstance.WorkflowStatus != WorkflowStatus.Running && workflowInstance.WorkflowStatus != WorkflowStatus.Idle)
            {
                _logger.LogDebug(
                    "Could not run workflow instance with ID {WorkflowInstanceId} because it has a status other than Idle, Suspended or Running. Its actual status is {WorkflowStatus}",
                    workflowInstanceId, workflowInstance.WorkflowStatus);
                return false;
            }

            if (workflowInstance.WorkflowStatus == WorkflowStatus.Idle)
                return true;

            if (activityId == null)
                return true;

            var activityIsBlocking = workflowInstance.BlockingActivities.Any(x => x.ActivityId == activityId);
            var activityIsScheduled = workflowInstance.ScheduledActivities.Any(x => x.ActivityId == activityId) || workflowInstance.CurrentActivity?.ActivityId == activityId;

            if (activityIsBlocking || activityIsScheduled)
                return true;

            _logger.LogWarning("Did not run workflow {WorkflowInstanceId} for activity {ActivityId} because the workflow is not blocked on that activity nor is that activity scheduled for execution", workflowInstanceId, activityId);
            return false;
        }
    }
}