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

        public async Task<PagedList<WorkflowDefinitionSummary>> ListAsync(ListWorkflowDefinitionsRequest request, CallContext context = default) =>
            await _elsaClient.WorkflowDefinitions.ListAsync(request.Page, request.PageSize, request.VersionOptions, context.CancellationToken);

        public async Task<WorkflowDefinition> GetByIdAsync(GetWorkflowDefinitionByIdRequest request, CallContext context = default) =>
            await _elsaClient.WorkflowDefinitions.GetByIdAsync(request.WorkflowDefinitionId, request.VersionOptions ?? VersionOptions.Published, context.CancellationToken);

        public Task<WorkflowDefinition> GetByVersionIdAsync(GetWorkflowDefinitionByVersionIdRequest request, CallContext context) => _elsaClient.WorkflowDefinitions.GetByVersionIdAsync(request.WorkflowDefinitionVersionId, context.CancellationToken);
        public async Task<WorkflowDefinition> SaveAsync(SaveWorkflowDefinitionRequest request, CallContext context) => await _elsaClient.WorkflowDefinitions.SaveAsync(request, context.CancellationToken);
        public async Task DeleteAsync(DeleteWorkflowDefinitionRequest request, CallContext context = default) => await _elsaClient.WorkflowDefinitions.DeleteAsync(request.WorkflowDefinitionId, context.CancellationToken);
    }
}