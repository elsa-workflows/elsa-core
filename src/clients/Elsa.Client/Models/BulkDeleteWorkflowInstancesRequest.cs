using System.Collections.Generic;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace ElsaDashboard.Shared.Rpc
{
    [DataContract]
    public class BulkDeleteWorkflowInstancesRequest
    {
        public BulkDeleteWorkflowInstancesRequest()
        {
        }

        public BulkDeleteWorkflowInstancesRequest(IEnumerable<string> workflowInstanceIds)
        {
            WorkflowInstanceIds = workflowInstanceIds;
        }

        [DataMember(Order = 1)] public IEnumerable<string> WorkflowInstanceIds { get; set; } = new string[0];
    }
}