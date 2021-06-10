using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Providers.WorkflowStorage
{
    /// <summary>
    /// Stores values within the workflow instance itself.
    /// </summary>
    public class WorkflowInstanceWorkflowStorageProvider : WorkflowStorageProvider
    {
        public override ValueTask SaveAsync(ActivityExecutionContext context, string propertyName, object? value, CancellationToken cancellationToken = default)
        {
            context.SetState(propertyName, value);
            return new ValueTask();
        }
        
        public override ValueTask<object?> LoadAsync(ActivityExecutionContext context, string propertyName, CancellationToken cancellationToken = default)
        {
            var value = context.GetState(propertyName);
            return new ValueTask<object?>(value);
        }

        public override ValueTask DeleteAsync(ActivityExecutionContext context, string propertyName, CancellationToken cancellationToken = default)
        {
            var state = context.GetData();
            state.Remove(propertyName);
            return new ValueTask();
        }

        public override ValueTask DeleteAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default)
        {
            context.WorkflowInstance.ActivityData.Remove(context.ActivityId);
            return new ValueTask();
        }
    }
}