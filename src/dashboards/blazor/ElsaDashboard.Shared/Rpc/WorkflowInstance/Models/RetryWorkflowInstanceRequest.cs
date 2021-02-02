using ProtoBuf;

// ReSharper disable once CheckNamespace
namespace ElsaDashboard.Shared.Rpc
{
    [ProtoContract]
    public class RetryWorkflowInstanceRequest
    {
        public RetryWorkflowInstanceRequest(string workflowInstanceId, bool runImmediately = false)
        {
            WorkflowInstanceId = workflowInstanceId;
            RunImmediately = runImmediately;
        }

        [ProtoMember(1)] public string WorkflowInstanceId { get; set; } = default!;
        [ProtoMember(2)] public bool RunImmediately { get; set; }
    }
}