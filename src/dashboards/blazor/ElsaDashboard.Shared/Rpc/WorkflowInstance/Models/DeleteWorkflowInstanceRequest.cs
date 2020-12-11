using ProtoBuf;

// ReSharper disable once CheckNamespace
namespace ElsaDashboard.Shared.Rpc
{
    [ProtoContract]
    public class DeleteWorkflowInstanceRequest
    {
        public DeleteWorkflowInstanceRequest()
        {
        }

        public DeleteWorkflowInstanceRequest(string workflowInstanceId)
        {
            WorkflowInstanceId = workflowInstanceId;
        }

        [ProtoMember(1)] public string WorkflowInstanceId { get; set; } = default!;
    }
}