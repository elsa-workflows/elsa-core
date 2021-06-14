using System.Threading;
using System.Threading.Tasks;
using Elsa.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Providers.WorkflowStorage
{
    /// <summary>
    /// Stores values in memory cache.
    /// </summary>
    public class TransientWorkflowStorageProvider : WorkflowStorageProvider
    {
        public const string ProviderName = "Transient";
        
        private readonly IMemoryCache _cache;
        private readonly ICacheSignal _cacheSignal;

        public TransientWorkflowStorageProvider(IMemoryCache cache, ICacheSignal cacheSignal)
        {
            _cache = cache;
            _cacheSignal = cacheSignal;
        }

        public override string Name => ProviderName;

        public override ValueTask SaveAsync(WorkflowStorageContext context, string key, object? value, CancellationToken cancellationToken = default)
        {
            var workflowInstanceId = context.WorkflowInstance.Id;
            var activityId = context.ActivityId;
            var cacheKey = GetKey(workflowInstanceId, activityId, key);
            var options = new MemoryCacheEntryOptions();

            options.ExpirationTokens.Add(_cacheSignal.GetToken(GetSignalKey(workflowInstanceId)));
            _cache.Set(cacheKey, value, options);

            return new ValueTask();
        }

        public override ValueTask<object?> LoadAsync(WorkflowStorageContext context, string key, CancellationToken cancellationToken = default)
        {
            var workflowInstanceId = context.WorkflowInstance.Id;
            var activityId = context.ActivityId;
            var cacheKey = GetKey(workflowInstanceId, activityId, key);
            var value = _cache.TryGetValue(cacheKey, out var v) ? v : default;
            return new ValueTask<object?>(value);
        }

        public override ValueTask DeleteAsync(WorkflowStorageContext context, string key, CancellationToken cancellationToken = default)
        {
            var workflowInstanceId = context.WorkflowInstance.Id;
            var activityId = context.ActivityId;
            var cacheKey = GetKey(workflowInstanceId, activityId, key);
            _cache.Remove(cacheKey);
            return new ValueTask();
        }

        public override ValueTask DeleteAsync(WorkflowStorageContext context, CancellationToken cancellationToken = default)
        {
            var workflowInstanceId = context.WorkflowInstance.Id;
            _cacheSignal.TriggerToken(GetSignalKey(workflowInstanceId));
            return new ValueTask();
        }

        private static (string workflowInstanceId, string? activityId, string propertyName) GetKey(string workflowInstanceId, string? activityId, string propertyName) => (workflowInstanceId, activityId, propertyName);
        private static string GetSignalKey(string workflowInstanceId) => $"{nameof(TransientWorkflowStorageProvider)}:{workflowInstanceId}";
    }
}