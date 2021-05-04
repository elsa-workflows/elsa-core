using Elsa.Client.Models;
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

        public ListWorkflowInstancesRequest(int page, int pageSize = 50, string? workflowDefinitionId = default, WorkflowStatus? workflowStatus = default, OrderBy? orderBy = default, string? searchTerm = default)
        {
            Page = page;
            PageSize = pageSize;
            WorkflowDefinitionId = workflowDefinitionId;
            WorkflowStatus = workflowStatus;
            OrderBy = orderBy;
            SearchTerm = searchTerm;
        }

        [ProtoMember(1)] public int Page { get; set; }
        [ProtoMember(2)] public int PageSize { get; set; } = 50;
        [ProtoMember(3)] public string? WorkflowDefinitionId { get; set; }
        [ProtoMember(4)] public WorkflowStatus? WorkflowStatus { get; set; }
        [ProtoMember(5)] public OrderBy? OrderBy { get; set; }
        [ProtoMember(6)] public string? SearchTerm { get; set; }
    }
}