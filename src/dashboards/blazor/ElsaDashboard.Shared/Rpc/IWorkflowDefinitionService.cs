using System.ServiceModel;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace ElsaDashboard.Shared.Rpc
{
    [Service]
    public interface IWorkflowDefinitionService
    {
        Task<PagedList<WorkflowDefinition>> ListAsync(ListWorkflowDefinitionsRequest request, CallContext context = default);
        Task<WorkflowDefinition> GetByVersionIdAsync(string workflowDefinitionVersionId, CallContext context = default);
        Task<WorkflowDefinition> SaveAsync(WorkflowDefinition workflowDefinition, CallContext context = default);
    }

    [ProtoContract]
    public record ListWorkflowDefinitionsRequest
    {
        public ListWorkflowDefinitionsRequest()
        {
        }
        
        public ListWorkflowDefinitionsRequest(int? page = default, int? pageSize = default, VersionOptions? versionOptions = default)
        {
            Page = page;
            PageSize = pageSize;
            VersionOptions = versionOptions;
        }

        [ProtoMember(1)] public int? Page { get; init; }
        [ProtoMember(2)] public int? PageSize { get; init; }
        [ProtoMember(3)] public VersionOptions? VersionOptions { get; init; }
    }

    public static class WorkflowDefinitionServiceExtensions
    {
        public static Task<PagedList<WorkflowDefinition>> ListAsync(this IWorkflowDefinitionService service, int? page = default, int? pageSize = default, VersionOptions? versionOptions = default)
        {
            return service.ListAsync(new ListWorkflowDefinitionsRequest(page, pageSize, versionOptions));
        }
    }
}