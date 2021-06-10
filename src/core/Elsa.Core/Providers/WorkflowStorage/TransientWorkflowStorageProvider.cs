using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Caching;
using Elsa.Services.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Providers.WorkflowStorage
{
    /// <summary>
    /// Stores values in memory cache.
    /// </summary>
    public class TransientWorkflowStorageProvider : WorkflowStorageProvider
    {
        private readonly IMemoryCache _cache;
        private readonly ICacheSignal _cacheSignal;

        public TransientWorkflowStorageProvider(IMemoryCache cache, ICacheSignal cacheSignal)
        {
            _cache = cache;
            _cacheSignal = cacheSignal;
        }

        public override ValueTask SaveAsync(ActivityExecutionContext context, string propertyName, object? value, CancellationToken cancellationToken = default)
        {
            var workflowInstanceId = context.WorkflowInstance.Id;
            var activityId = context.ActivityId;
            var key = GetKey(workflowInstanceId, activityId, propertyName);
            var options = new MemoryCacheEntryOptions();

            options.ExpirationTokens.Add(_cacheSignal.GetToken(GetSignalKey(workflowInstanceId)));
            _cache.Set(key, value, options);

            return new ValueTask();
        }

        public override ValueTask<object?> LoadAsync(ActivityExecutionContext context, string propertyName, CancellationToken cancellationToken = default)
        {
            var workflowInstanceId = context.WorkflowInstance.Id;
            var activityId = context.ActivityId;
            var key = GetKey(workflowInstanceId, activityId, propertyName);
            var value = _cache.TryGetValue(key, out var v) ? v : default;
            return new ValueTask<object?>(value);
        }

        public override ValueTask DeleteAsync(ActivityExecutionContext context, string propertyName, CancellationToken cancellationToken = default)
        {
            var workflowInstanceId = context.WorkflowInstance.Id;
            var activityId = context.ActivityId;
            var key = GetKey(workflowInstanceId, activityId, propertyName);
            _cache.Remove(key);
            return new ValueTask();
        }

        public override ValueTask DeleteAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default)
        {
            var workflowInstanceId = context.WorkflowInstance.Id;
            _cacheSignal.TriggerToken(GetSignalKey(workflowInstanceId));
            return new ValueTask();
        }

        private static (string workflowInstanceId, string activityId, string propertyName) GetKey(string workflowInstanceId, string activityId, string propertyName) => (workflowInstanceId, activityId, propertyName);
        private static string GetSignalKey(string workflowInstanceId) => $"{nameof(TransientWorkflowStorageProvider)}:{workflowInstanceId}";
    }
}