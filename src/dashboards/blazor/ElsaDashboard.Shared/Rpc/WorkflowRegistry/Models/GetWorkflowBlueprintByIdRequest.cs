using Elsa.Client.Models;
using ProtoBuf;

namespace ElsaDashboard.Shared.Rpc
{
    [ProtoContract]
    public class GetWorkflowBlueprintByIdRequest
    {
        public GetWorkflowBlueprintByIdRequest()
        {
        }

        public GetWorkflowBlueprintByIdRequest(string id, VersionOptions versionOptions)
        {
            Id = id;
            VersionOptions = versionOptions;
        }

        [ProtoMember(1)] public string Id { get; set; } = default!;
        [ProtoMember(2)] public VersionOptions VersionOptions { get; set; }
    }
}