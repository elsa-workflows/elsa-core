using System.Diagnostics;
using System.Threading.Tasks;
using Elsa.Client;
using Elsa.Client.Models;
using ElsaDashboard.Shared.Rpc;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;
using RetryWorkflowRequest = ElsaDashboard.Shared.Rpc.RetryWorkflowRequest;

namespace ElsaDashboard.Backend.Rpc
{
    public class WorkflowInstanceService : IWorkflowInstanceService
    {
        private readonly IElsaClient _elsaClient;
        private readonly ILogger<WorkflowInstanceService> _logger;
        private readonly Stopwatch _stopwatch = new();

        public WorkflowInstanceService(IElsaClient elsaClient, ILogger<WorkflowInstanceService> logger)
        {
            _elsaClient = elsaClient;
            _logger = logger;
        }

        public async Task<PagedList<WorkflowInstanceSummary>> ListAsync(ListWorkflowInstancesRequest request, CallContext context = default)
        {
            _stopwatch.Restart();
            var result = await _elsaClient.WorkflowInstances.ListAsync(request.Page, request.PageSize, request.WorkflowDefinitionId, request.WorkflowStatus, request.OrderBy, request.SearchTerm, context.CancellationToken);
            _stopwatch.Stop();
            _logger.LogDebug("ListAsync finished in {TimeElapsed}", _stopwatch.Elapsed);
            return result;
        }

        public Task<WorkflowInstance?> GetByIdAsync(GetWorkflowInstanceByIdRequest request, CallContext context = default) => _elsaClient.WorkflowInstances.GetByIdAsync(request.WorkflowInstanceId, context.CancellationToken);
        public Task DeleteAsync(DeleteWorkflowRequest request, CallContext context = default) => _elsaClient.WorkflowInstances.DeleteAsync(request.WorkflowInstanceId, context.CancellationToken);
        public Task BulkDeleteAsync(BulkDeleteWorkflowInstancesRequest request, CallContext context = default) => _elsaClient.WorkflowInstances.BulkDeleteAsync(request, context.CancellationToken);

        public Task RetryAsync(RetryWorkflowRequest request, CallContext context = default) => _elsaClient.WorkflowInstances.RetryAsync(request.WorkflowInstanceId, new Elsa.Client.Models.RetryWorkflowRequest(request.RunImmediately), context.CancellationToken);
        public Task BulkRetryAsync(BulkRetryWorkflowInstancesRequest request, CallContext context = default) => _elsaClient.WorkflowInstances.BulkRetryAsync(request, context.CancellationToken);
    }
}