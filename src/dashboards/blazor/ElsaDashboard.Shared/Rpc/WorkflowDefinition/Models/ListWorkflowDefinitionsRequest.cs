using Elsa.Client.Models;
using ProtoBuf;

namespace ElsaDashboard.Shared.Rpc
{
    [ProtoContract]
    public class ListWorkflowDefinitionsRequest
    {
        public ListWorkflowDefinitionsRequest()
        {
        }

        public ListWorkflowDefinitionsRequest(int? page, int? pageSize, VersionOptions? versionOptions)
        {
            Page = page;
            PageSize = pageSize;
            VersionOptions = versionOptions;
        }

        [ProtoMember(1)]  public int? Page { get; set; }
        [ProtoMember(2)]  public int? PageSize { get; set; }
        [ProtoMember(3)]  public VersionOptions? VersionOptions { get; set; }
    }
}