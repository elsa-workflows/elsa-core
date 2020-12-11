using ProtoBuf;

namespace ElsaDashboard.Shared.Rpc
{
    [ProtoContract]
    public class GetWorkflowDefinitionByVersionIdRequest
    {
        public GetWorkflowDefinitionByVersionIdRequest()
        {
        }

        public GetWorkflowDefinitionByVersionIdRequest(string workflowDefinitionVersionId)
        {
            WorkflowDefinitionVersionId = workflowDefinitionVersionId;
        }

        [ProtoMember(1)] public string WorkflowDefinitionVersionId { get; set; } = default!;
    }
}