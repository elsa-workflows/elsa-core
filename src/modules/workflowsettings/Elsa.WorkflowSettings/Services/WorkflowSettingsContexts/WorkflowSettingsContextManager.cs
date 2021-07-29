using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;
using Elsa.WorkflowSettings.Abstractions.Persistence;
using Elsa.WorkflowSettings.Abstractions.Services.WorkflowSettingsContexts;
using Elsa.WorkflowSettings.Extensions;

namespace Elsa.WorkflowSettings.Services.WorkflowSettingsContexts
{
    public class WorkflowSettingsContextManager : IWorkflowSettingsContextManager
    {
        private readonly IWorkflowSettingsStore _workflowSettingsStore;

        public WorkflowSettingsContextManager(IWorkflowSettingsStore workflowSettingsStore)
        {
            _workflowSettingsStore = workflowSettingsStore;
        }

        public async ValueTask<bool> LoadContext(WorkflowSettingsContext context, CancellationToken cancellationToken = default)
        {
            var workflowBlueprintId = context.WorkflowBlueprintId;
            var key = context.Key;

            var workflowSettings = await _workflowSettingsStore.FindByWorkflowBlueprintIdAndKeyAsync(workflowBlueprintId, key, cancellationToken);

            return workflowSettings != null
                ? bool.Parse(workflowSettings.Value)
                : false;
        }

        //private async 
        
    }
}
