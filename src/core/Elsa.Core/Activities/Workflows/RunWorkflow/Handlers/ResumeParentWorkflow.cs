using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services;
using Elsa.Services.WorkflowStorage;
using MediatR;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Workflows
{
    public class ResumeParentWorkflow : INotificationHandler<WorkflowCompleted>
    {
        private readonly IFindsAndResumesWorkflows _workflowScheduler;
        private readonly IWorkflowStorageService _workflowStorageService;

        public ResumeParentWorkflow(IFindsAndResumesWorkflows workflowScheduler, IWorkflowStorageService workflowStorageService)
        {
            _workflowScheduler = workflowScheduler;
            _workflowStorageService = workflowStorageService;
        }

        public async Task Handle(WorkflowCompleted notification, CancellationToken cancellationToken)
        {
            var workflowExecutionContext = notification.WorkflowExecutionContext;
            var workflowInstance = workflowExecutionContext.WorkflowInstance;
            var workflowInstanceId = workflowExecutionContext.WorkflowInstance.Id;
            var outputReference = workflowExecutionContext.WorkflowInstance.Output;
            var output = outputReference != null ? await _workflowStorageService.LoadAsync(outputReference.ProviderName, new WorkflowStorageContext(workflowInstance, outputReference.ActivityId), "Output", cancellationToken) : null;
            var tenantId = workflowExecutionContext.WorkflowInstance.TenantId;

            var input = new FinishedWorkflowModel
            {
                WorkflowInstanceId = workflowInstanceId,
                WorkflowOutput = output
            };

            var trigger = new RunWorkflowBookmark
            {
                ChildWorkflowInstanceId = workflowInstanceId
            };

            await _workflowScheduler.FindAndResumeWorkflowsAsync(nameof(RunWorkflow), trigger, tenantId, new WorkflowInput(input), cancellationToken: cancellationToken);
        }
    }
}