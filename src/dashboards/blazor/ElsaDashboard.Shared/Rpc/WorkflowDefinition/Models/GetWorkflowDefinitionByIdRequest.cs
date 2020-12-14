using Elsa.Client.Models;
using ProtoBuf;

namespace ElsaDashboard.Shared.Rpc
{
    [ProtoContract]
    public class GetWorkflowDefinitionByIdRequest
    {
        public GetWorkflowDefinitionByIdRequest()
        {
        }

        public GetWorkflowDefinitionByIdRequest(string workflowDefinitionId, VersionOptions? versionOptions)
        {
            WorkflowDefinitionId = workflowDefinitionId;
            VersionOptions = versionOptions;
        }

        [ProtoMember(1)]  public string WorkflowDefinitionId { get; set; } = default!;
        [ProtoMember(2)]  public VersionOptions? VersionOptions { get; set; }
    }
}