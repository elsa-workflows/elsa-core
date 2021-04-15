using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elsa.Dispatch.Handlers
{
    public class ExecuteWorkflowInstance : IRequestHandler<ExecuteWorkflowInstanceRequest>
    {
        private readonly IResumesWorkflow _workflowRunner;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly ILogger _logger;

        public ExecuteWorkflowInstance(IResumesWorkflow workflowRunner, IWorkflowInstanceStore workflowInstanceStore, ILogger<ExecuteWorkflowInstance> logger)
        {
            _workflowRunner = workflowRunner;
            _workflowInstanceStore = workflowInstanceStore;
            _logger = logger;
        }

        public async Task<Unit> Handle(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken)
        {
            var workflowInstanceId = request.WorkflowInstanceId;
            var workflowInstance = await _workflowInstanceStore.FindByIdAsync(request.WorkflowInstanceId, cancellationToken);

            if (!ValidatePreconditions(workflowInstanceId, workflowInstance, request.ActivityId))
                return Unit.Value;

            await _workflowRunner.ResumeWorkflowAsync(
                workflowInstance!,
                request.ActivityId,
                request.Input, cancellationToken);

            return Unit.Value;
        }

        private bool ValidatePreconditions(string? workflowInstanceId, WorkflowInstance? workflowInstance, string? activityId)
        {
            if (workflowInstance == null)
            {
                _logger.LogWarning("Could not run workflow instance with ID {WorkflowInstanceId} because it does not exist", workflowInstanceId);
                return false;
            }

            if (workflowInstance.WorkflowStatus != WorkflowStatus.Suspended && workflowInstance.WorkflowStatus != WorkflowStatus.Running)
            {
                _logger.LogWarning(
                    "Could not run workflow instance with ID {WorkflowInstanceId} because it has a status other than Suspended or Running. Its actual status is {WorkflowStatus}",
                    workflowInstanceId, workflowInstance.WorkflowStatus);
                return false;
            }

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