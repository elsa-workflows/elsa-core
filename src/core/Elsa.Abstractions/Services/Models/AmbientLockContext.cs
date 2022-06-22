using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Medallion.Threading;

namespace Elsa.Services.Models
{
    public static class AmbientLockContext
    {
        private static readonly AsyncLocal<IDistributedSynchronizationHandle?> CorrelationLock = new();
        private static readonly AsyncLocal<ConcurrentDictionary<string, IDistributedSynchronizationHandle?>> WorkflowInstanceLocks = new();

        public static IDistributedSynchronizationHandle? CurrentCorrelationLock
        {
            get => CorrelationLock.Value;
            set => CorrelationLock.Value = value;
        }
        
        public static IDistributedSynchronizationHandle? GetCurrentWorkflowInstanceLock(string workflowInstanceId) => 
            WorkflowInstanceLocks.Value != null ? WorkflowInstanceLocks.Value.TryGetValue(workflowInstanceId, out var handle) ? handle : default : default;

        public static void SetCurrentWorkflowInstanceLock(string workflowInstanceId, IDistributedSynchronizationHandle handle)
        {
            var dictionary = WorkflowInstanceLocks.Value ?? new ConcurrentDictionary<string, IDistributedSynchronizationHandle?>();
            dictionary.AddOrUpdate(workflowInstanceId, _ => handle, (_, _) => handle);
            WorkflowInstanceLocks.Value = dictionary;
        }

        public static void DeleteCurrentWorkflowInstanceLock(string workflowInstanceId) => WorkflowInstanceLocks.Value.Remove(workflowInstanceId, out _);
    }
}