using System.Threading.Tasks;
using Elsa.Client;
using Elsa.Client.Models;
using ElsaDashboard.Shared.Rpc;
using ProtoBuf.Grpc;

namespace ElsaDashboard.Backend.Rpc
{
    public class WorkflowRegistryService : IWorkflowRegistryService
    {
        private readonly IElsaClient _elsaClient;
        public WorkflowRegistryService(IElsaClient elsaClient) => _elsaClient = elsaClient;

        public Task<PagedList<WorkflowBlueprint>> ListAsync(int? page = default, int? pageSize = default, VersionOptions? versionOptions = default, CallContext context = default) =>
            _elsaClient.WorkflowRegistry.ListAsync(page, pageSize, versionOptions, context.CancellationToken);

        public Task<WorkflowBlueprint> GetById(string id, VersionOptions versionOptions, CallContext context = default) => _elsaClient.WorkflowRegistry.GetByIdAsync(id, versionOptions, context.CancellationToken);
    }
}