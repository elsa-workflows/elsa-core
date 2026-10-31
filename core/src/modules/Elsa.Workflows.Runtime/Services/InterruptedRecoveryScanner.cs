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
    private readonly ITenantScopeFactory? _tenantScopeFactory;

    [Obsolete("Use the constructor that accepts an ITenantScopeFactory instead. Without it, tenants with their own database are not recovered. Removed in the next major version.")]
    public InterruptedRecoveryScanner(
        IWorkflowRestarter restarter,
        IWorkflowInstanceStore instanceStore,
        IOptions<RuntimeOptions> runtimeOptions,
        ILogger<InterruptedRecoveryScanner> logger,
        ITenantService? tenantService = null,
        ITenantAccessor? tenantAccessor = null) : this(restarter, instanceStore, runtimeOptions, logger, tenantService, tenantAccessor, null)
    {
    }

    public InterruptedRecoveryScanner(
        IWorkflowRestarter restarter,
        IWorkflowInstanceStore instanceStore,
        IOptions<RuntimeOptions> runtimeOptions,
        ILogger<InterruptedRecoveryScanner> logger,
        ITenantService? tenantService,
        ITenantAccessor? tenantAccessor,
        ITenantScopeFactory? tenantScopeFactory = null)
    {
        _restarter = restarter;
        _instanceStore = instanceStore;
        _runtimeOptions = runtimeOptions;
        _logger = logger;
        _tenantService = tenantService;
        _tenantAccessor = tenantAccessor;
        _tenantScopeFactory = tenantScopeFactory;
    }

    /// <inheritdoc />
    public async ValueTask<int> ScanAndRequeueAsync(CancellationToken cancellationToken)
    {
        var batchSize = _runtimeOptions.Value.RestartInterruptedWorkflowsBatchSize;

        _logger.LogInformation("Scanning for workflows interrupted by graceful drain.");

        var requeued = await OwnDatabaseTenantRecovery.RequeueAsync(
            (store, tenantAgnostic) => store.EnumerateSummariesAsync(CreateFilter(tenantAgnostic), batchSize, cancellationToken),
            _instanceStore,
            _restarter,
            _tenantService,
            _tenantAccessor,
            _tenantScopeFactory,
            _logger,
            cancellationToken);

        _logger.LogInformation("Interrupted-workflow scan complete; requeued {Count} instance(s).", requeued);
        return requeued;
    }

    private static WorkflowInstanceFilter CreateFilter(bool tenantAgnostic) => new()
    {
        WorkflowSubStatus = WorkflowSubStatus.Interrupted,
        WorkflowStatus = WorkflowStatus.Running,
        TenantAgnostic = tenantAgnostic,
    };
}
