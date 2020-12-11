using ProtoBuf;

// ReSharper disable once CheckNamespace
namespace ElsaDashboard.Shared.Rpc
{
    [ProtoContract]
    public class ListWorkflowInstancesRequest
    {
        public ListWorkflowInstancesRequest()
        {
        }

        public ListWorkflowInstancesRequest(int page, int pageSize = 50)
        {
            Page = page;
            PageSize = pageSize;
        }

        [ProtoMember(1)] public int Page { get; set; }
        [ProtoMember(2)] public int PageSize { get; set; } = 50;
    }
}