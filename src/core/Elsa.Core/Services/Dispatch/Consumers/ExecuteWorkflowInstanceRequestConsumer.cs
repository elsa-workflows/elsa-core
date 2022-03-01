using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Rebus.Handlers;

namespace Elsa.Services.Dispatch.Consumers
{
    public class ExecuteWorkflowInstanceRequestConsumer : IHandleMessages<ExecuteWorkflowInstanceRequest>
    {
        private readonly IWorkflowInstanceExecutor _workflowInstanceExecutor;
        private readonly ITenantProvider _tenantProvider;

        public ExecuteWorkflowInstanceRequestConsumer(IWorkflowInstanceExecutor workflowInstanceExecutor, ITenantProvider tenantProvider)
        {
            _workflowInstanceExecutor = workflowInstanceExecutor;
            _tenantProvider = tenantProvider;
        }

        public async Task Handle(ExecuteWorkflowInstanceRequest message)
        {
            if (message.Tenant != null) await _tenantProvider.SetCurrentTenantAsync(message.Tenant);

            await _workflowInstanceExecutor.ExecuteAsync(message.WorkflowInstanceId, message.ActivityId, message.Input);
        }
    }
}