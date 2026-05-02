using Elsa.Common;
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
        var filter = new WorkflowInstanceFilter { WorkflowSubStatus = WorkflowSubStatus.Interrupted };
        var batchSize = _runtimeOptions.Value.RestartInterruptedWorkflowsBatchSize;
        var instances = _instanceStore.EnumerateSummariesAsync(filter, batchSize, cancellationToken);
        var requeued = 0;

        _logger.LogInformation("Scanning for workflows interrupted by graceful drain.");

        await foreach (var summary in instances.WithCancellation(cancellationToken))
        {
            try
            {
                var tenantId = summary.TenantId ?? string.Empty;

                if (_tenantService is not null && _tenantAccessor is not null && !string.IsNullOrWhiteSpace(tenantId) && tenantId != Tenant.AgnosticTenantId)
                {
                    var tenant = await _tenantService.FindAsync(tenantId, cancellationToken) ?? new Tenant { Id = tenantId, Name = tenantId };

                    using (_tenantAccessor.PushContext(tenant))
                        await _restarter.RestartWorkflowAsync(summary.Id, cancellationToken);
                }
                else
                {
                    await _restarter.RestartWorkflowAsync(summary.Id, cancellationToken);
                }

                requeued++;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.LogError(ex, "Failed to requeue interrupted workflow {WorkflowInstanceId}; will be retried by the timeout-based recovery task.", summary.Id);
            }
        }

        _logger.LogInformation("Interrupted-workflow scan complete; requeued {Count} instance(s).", requeued);
        return requeued;
    }
}
