using System.Threading.Tasks;
using Elsa.Client.Models;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace ElsaDashboard.Shared.Rpc
{
    [Service]
    public interface IWorkflowDefinitionService
    {
        Task<PagedList<WorkflowDefinition>> ListAsync(int? page = default, int? pageSize = default, VersionOptions? versionOptions = default, CallContext context = default);
        Task<WorkflowDefinition> GetById(string workflowDefinitionId, VersionOptions versionOptions, CallContext context = default);
        Task<WorkflowDefinition> GetByVersionIdAsync(string workflowDefinitionVersionId, CallContext context = default);
        Task<WorkflowDefinition> SaveAsync(SaveWorkflowDefinitionRequest request, CallContext context = default);
    }
}