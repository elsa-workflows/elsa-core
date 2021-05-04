using ProtoBuf;

namespace ElsaDashboard.Shared.Rpc
{
    [ProtoContract]
    public class DeleteWorkflowDefinitionRequest
    {
        public DeleteWorkflowDefinitionRequest()
        {
        }

        public DeleteWorkflowDefinitionRequest(string workflowDefinitionId)
        {
            WorkflowDefinitionId = workflowDefinitionId;
        }

        [ProtoMember(1)] public string WorkflowDefinitionId { get; set; } = default!;
    }
}