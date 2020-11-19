using System.Threading.Tasks;
using Elsa.Client;
using Elsa.Client.Models;
using ElsaDashboard.Shared.Rpc;
using ProtoBuf.Grpc;

namespace ElsaDashboard.Backend.Rpc
{
    public class WorkflowDefinitionService : IWorkflowDefinitionService
    {
        private readonly IElsaClient _elsaClient;

        public WorkflowDefinitionService(IElsaClient elsaClient)
        {
            _elsaClient = elsaClient;
        }

        public async Task<PagedList<WorkflowDefinition>> ListAsync(int? page, int? pageSize, VersionOptions? versionOptions, CallContext context) =>
            await _elsaClient.WorkflowDefinitions.ListAsync(page, pageSize, versionOptions, context.CancellationToken);

        public async Task<WorkflowDefinition> GetById(string workflowDefinitionId, VersionOptions versionOptions, CallContext context = default) =>
            await _elsaClient.WorkflowDefinitions.GetByIdAsync(workflowDefinitionId, versionOptions, context.CancellationToken);

        public Task<WorkflowDefinition> GetByVersionIdAsync(string workflowDefinitionVersionId, CallContext context) => _elsaClient.WorkflowDefinitions.GetByVersionIdAsync(workflowDefinitionVersionId, context.CancellationToken);
        public async Task<WorkflowDefinition> SaveAsync(SaveWorkflowDefinitionRequest request, CallContext context) => await _elsaClient.WorkflowDefinitions.SaveAsync(request, context.CancellationToken);
    }
}