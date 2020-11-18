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

        public async Task<PagedList<WorkflowDefinition>> ListAsync(ListWorkflowDefinitionsRequest request, CallContext context) =>
            await _elsaClient.WorkflowDefinitions.ListAsync(request.Page, request.PageSize, request.VersionOptions, context.CancellationToken);

        public Task<WorkflowDefinition> GetByVersionIdAsync(string workflowDefinitionVersionId, CallContext context) => _elsaClient.WorkflowDefinitions.GetByVersionIdAsync(workflowDefinitionVersionId, context.CancellationToken);
        public async Task<WorkflowDefinition> SaveAsync(WorkflowDefinition workflowDefinition, CallContext context) => await _elsaClient.WorkflowDefinitions.PostAsync(workflowDefinition, context.CancellationToken);
    }
}