using System.Threading;
using System.Threading.Tasks;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.Models;
using Elsa.Services.WorkflowStorage;

namespace Elsa.ActivityResults
{
    public class OutputResult : ActivityExecutionResult
    {
        public OutputResult(object? output, string? storageProviderName = default)
        {
            Output = output;
            StorageProviderName = storageProviderName;
        }

        public object? Output { get; }
        public string? StorageProviderName { get; }
        
        public override async ValueTask ExecuteAsync(ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var workflowStorageService = activityExecutionContext.GetService<IWorkflowStorageService>();
            var workflowStorageContext = new WorkflowStorageContext(activityExecutionContext.WorkflowInstance, activityExecutionContext.ActivityId);
            await workflowStorageService.SaveAsync(StorageProviderName, workflowStorageContext, ActivityOutput.PropertyName, Output, cancellationToken);
            activityExecutionContext.WorkflowInstance.Output = new WorkflowOutputReference(StorageProviderName, workflowStorageContext.ActivityId);
        }
    }
}