using ProtoBuf;

// ReSharper disable once CheckNamespace
namespace ElsaDashboard.Shared.Rpc
{
    [ProtoContract]
    public class DeleteWorkflowRequest
    {
        public DeleteWorkflowRequest()
        {
        }

        public DeleteWorkflowRequest(string workflowInstanceId)
        {
            WorkflowInstanceId = workflowInstanceId;
        }

        [ProtoMember(1)] public string WorkflowInstanceId { get; set; } = default!;
    }
}