using ProtoBuf;

// ReSharper disable once CheckNamespace
namespace ElsaDashboard.Shared.Rpc
{
    [ProtoContract]
    public class RetryWorkflowRequest
    {
        public RetryWorkflowRequest(string workflowInstanceId, bool runImmediately = false)
        {
            WorkflowInstanceId = workflowInstanceId;
            RunImmediately = runImmediately;
        }

        [ProtoMember(1)] public string WorkflowInstanceId { get; set; }
        [ProtoMember(2)] public bool RunImmediately { get; set; }
    }
}