using ProtoBuf;

// ReSharper disable once CheckNamespace
namespace ElsaDashboard.Shared.Rpc
{
    [ProtoContract]
    public class GetWorkflowInstanceByIdRequest
    {
        public GetWorkflowInstanceByIdRequest()
        {
        }

        public GetWorkflowInstanceByIdRequest(string workflowInstanceId)
        {
            WorkflowInstanceId = workflowInstanceId;
        }

        [ProtoMember(1)] public string WorkflowInstanceId { get; set; } = default!;
    }
}