using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Providers.WorkflowStorage
{
    /// <summary>
    /// Stores values within the workflow instance itself.
    /// </summary>
    public class WorkflowInstanceWorkflowStorageProvider : WorkflowStorageProvider
    {
        public const string ProviderName = "WorkflowInstance";
        
        public override string Name => ProviderName;
        
        public override ValueTask SaveAsync(WorkflowStorageContext context, string key, object? value, CancellationToken cancellationToken = default)
        {
            SetState(context, key, value);
            return new ValueTask();
        }
        
        public override ValueTask<object?> LoadAsync(WorkflowStorageContext context, string key, CancellationToken cancellationToken = default)
        {
            var value = GetState(context, key);
            return new ValueTask<object?>(value);
        }

        public override ValueTask DeleteAsync(WorkflowStorageContext context, string key, CancellationToken cancellationToken = default)
        {
            var state = GetData(context);
            state.Remove(key);
            return new ValueTask();
        }

        public override ValueTask DeleteAsync(WorkflowStorageContext context, CancellationToken cancellationToken = default)
        {
            context.WorkflowInstance.ActivityData.Remove(context.ActivityId);
            return new ValueTask();
        }

        private IDictionary<string, object> GetData(WorkflowStorageContext context) => context.WorkflowInstance.ActivityData.GetItem(context.ActivityId, () => new Dictionary<string, object>());
        private void SetState(WorkflowStorageContext context, string propertyName, object? value) => GetData(context)!.SetState(propertyName, value);
        public object? GetState(WorkflowStorageContext context, string propertyName) => GetData(context)!.GetState(propertyName);
    }
}