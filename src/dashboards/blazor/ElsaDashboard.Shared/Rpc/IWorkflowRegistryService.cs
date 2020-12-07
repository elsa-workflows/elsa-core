using System.Threading.Tasks;
using Elsa.Client.Models;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace ElsaDashboard.Shared.Rpc
{
    [Service]
    public interface IWorkflowRegistryService
    {
        Task<PagedList<WorkflowBlueprint>> ListAsync(int? page = default, int? pageSize = default, VersionOptions? versionOptions = default, CallContext context = default);
        Task<WorkflowBlueprint> GetById(string id, VersionOptions versionOptions, CallContext context = default);
    }
}