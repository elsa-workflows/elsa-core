using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Default implementation of <see cref="IInterruptedRecoveryScanner"/>.
/// </summary>
public sealed class InterruptedRecoveryScanner : IInterruptedRecoveryScanner
{
    private readonly IWorkflowRestarter _restarter;
    private readonly IWorkflowInstanceStore _instanceStore;
    private readonly IOptions<RuntimeOptions> _runtimeOptions;
    private readonly ILogger<InterruptedRecoveryScanner> _logger;
    private readonly ITenantService? _tenantService;
    private readonly ITenantAccessor? _tenantAccessor;

    public InterruptedRecoveryScanner(
        IWorkflowRestarter restarter,
        IWorkflowInstanceStore instanceStore,
        IOptions<RuntimeOptions> runtimeOptions,
        ILogger<InterruptedRecoveryScanner> logger,
        ITenantService? tenantService = null,
        ITenantAccessor? tenantAccessor = null)
    {
        _restarter = restarter;
        _instanceStore = instanceStore;
        _runtimeOptions = runtimeOptions;
        _logger = logger;
        _tenantService = tenantService;
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    public async ValueTask<int> ScanAndRequeueAsync(CancellationToken cancellationToken)
    {
        var filter = new WorkflowInstanceFilter
        {
            WorkflowSubStatus = WorkflowSubStatus.Interrupted,
            WorkflowStatus = WorkflowStatus.Running,
            TenantAgnostic = true,
        };
        var batchSize = _runtimeOptions.Value.RestartInterruptedWorkflowsBatchSize;
        var instances = _instanceStore.EnumerateSummariesAsync(filter, batchSize, cancellationToken);

        _logger.LogInformation("Scanning for workflows interrupted by graceful drain.");

        var requeued = await InterruptedWorkflowRequeuer.RequeueAsync(instances, _restarter, _tenantService, _tenantAccessor, _logger, cancellationToken);

        _logger.LogInformation("Interrupted-workflow scan complete; requeued {Count} instance(s).", requeued);
        return requeued;
    }
}
