using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Events;
using Elsa.Models;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.Models;
using Elsa.Services.WorkflowStorage;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Services.Workflows
{
    public class MultitenantWorkflowRunner : WorkflowRunner
    {
        private readonly ITenantProvider _tenantProvider;

        public MultitenantWorkflowRunner(
            IWorkflowContextManager workflowContextManager,
            IMediator mediator,
            IServiceScopeFactory serviceScopeFactory,
            IGetsStartActivities startingActivitiesProvider,
            IWorkflowStorageService workflowStorageService,
            ILogger<MultitenantWorkflowRunner> logger,
            ITenantProvider tenantProvider): base(workflowContextManager, mediator, serviceScopeFactory, startingActivitiesProvider, workflowStorageService, logger)
        {
            _tenantProvider = tenantProvider;
        }

        public override async Task<RunWorkflowResult> RunWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            WorkflowInput? input = default,
            CancellationToken cancellationToken = default)
        {
            using var loggingScope = _logger.BeginScope(new Dictionary<string, object> { ["WorkflowInstanceId"] = workflowInstance.Id });

            var tenant = _tenantProvider.GetCurrentTenant();

            using var workflowExecutionScope = _serviceScopeFactory.CreateScopeForTenant(tenant);

            if (input?.Input != null)
            {
                var workflowStorageContext = new WorkflowStorageContext(workflowInstance, workflowBlueprint.Id);
                var inputStorageProvider = _workflowStorageService.GetProviderByNameOrDefault(input.StorageProviderName);
                await inputStorageProvider.SaveAsync(workflowStorageContext, nameof(WorkflowInstance.Input), input.Input, cancellationToken);
                workflowInstance.Input = new WorkflowInputReference(inputStorageProvider.Name);
                await _mediator.Publish(new WorkflowInputUpdated(workflowInstance), cancellationToken);
            }

            var workflowExecutionContext = new WorkflowExecutionContext(workflowExecutionScope.ServiceProvider, workflowBlueprint, workflowInstance, input?.Input);
            var result = await RunWorkflowInternalAsync(workflowExecutionContext, activityId, cancellationToken);
            await workflowExecutionContext.WorkflowExecutionLog.FlushAsync(cancellationToken);
            return result;
        }
    }
}
