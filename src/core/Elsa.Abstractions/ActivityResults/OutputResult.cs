using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;
using Elsa.Services.WorkflowStorage;

namespace Elsa.ActivityResults
{
    public class OutputResult : ActivityExecutionResult
    {
        public OutputResult(object? output, string? storageName = default)
        {
            Output = output;
            StorageName = storageName;
        }

        public object? Output { get; }
        public string? StorageName { get; }
        public override async ValueTask ExecuteAsync(ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var workflowStorageService = activityExecutionContext.GetService<IWorkflowStorageService>();
            var provider = workflowStorageService.GetProviderByNameOrDefault(StorageName);
            await provider.SaveAsync(activityExecutionContext, ActivityOutput.PropertyName, Output, cancellationToken);
        }
    }
}