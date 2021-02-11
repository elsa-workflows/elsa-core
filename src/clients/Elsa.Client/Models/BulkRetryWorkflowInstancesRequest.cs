using System.Collections.Generic;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace ElsaDashboard.Shared.Rpc
{
    [DataContract]
    public class BulkRetryWorkflowInstancesRequest
    {
        public BulkRetryWorkflowInstancesRequest(IEnumerable<string> workflowInstanceIds, bool runImmediately = false)
        {
            WorkflowInstanceIds = workflowInstanceIds;
            RunImmediately = runImmediately;
        }

        [DataMember(Order = 1)] public IEnumerable<string> WorkflowInstanceIds { get; set; }
        [DataMember(Order = 2)] public bool RunImmediately { get; set; }
    }
}