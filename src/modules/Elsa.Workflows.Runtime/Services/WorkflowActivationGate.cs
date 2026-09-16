using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Elsa.Common.DistributedHosting;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.ActivationValidators;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.ActivationValidators;
using Medallion.Threading;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowActivationGate(
    IWorkflowActivationStrategyEvaluator evaluator,
    IDistributedLockProvider distributedLockProvider,
    IOptions<DistributedLockingOptions> distributedLockingOptions,
    ITenantAccessor tenantAccessor) : IWorkflowActivationGate
{
    /// <inheritdoc />
    public async Task<WorkflowActivationLease> EvaluateAsync(Workflow workflow, string? correlationId, CancellationToken cancellationToken = default)
    {
        var lockKey = GetLockKey(workflow, correlationId);
        IDistributedSynchronizationHandle? lockHandle = null;

        if (lockKey != null)
            lockHandle = await distributedLockProvider.AcquireLockAsync(lockKey, distributedLockingOptions.Value.LockAcquisitionTimeout, cancellationToken);

        try
        {
            var canStart = await evaluator.CanStartWorkflowAsync(new()
            {
                Workflow = workflow,
                CorrelationId = correlationId,
                CancellationToken = cancellationToken
            });

            if (!canStart)
            {
                if (lockHandle != null)
                    await lockHandle.DisposeAsync();

                return WorkflowActivationLease.Denied;
            }

            return new(true, lockHandle);
        }
        catch
        {
            if (lockHandle != null)
                await lockHandle.DisposeAsync();

            throw;
        }
    }

    private string? GetLockKey(Workflow workflow, string? correlationId)
    {
        var strategyType = workflow.Options.ActivationStrategyType;

        if (strategyType == null || strategyType == typeof(AllowAlwaysStrategy))
            return null;

        var tenantId = tenantAccessor.TenantId;
        var definitionId = workflow.Identity.DefinitionId;

        if (strategyType == typeof(SingletonStrategy))
            return CreateLockKey("singleton", tenantId, definitionId);

        if (strategyType == typeof(CorrelatedSingletonStrategy))
        {
            return CreateLockKey("correlated-singleton", tenantId, definitionId, correlationId ?? string.Empty);
        }

        if (strategyType == typeof(CorrelationStrategy))
        {
            return CreateLockKey("correlation", tenantId, correlationId ?? string.Empty);
        }

        // Custom strategies remain compatible, but their uniqueness scope is unknown. Do not
        // invent a lock key that implies atomicity the strategy has not declared.
        return null;
    }

    private static string CreateLockKey(string strategy, params string[] components)
    {
        using var stream = new MemoryStream();
        Span<byte> length = stackalloc byte[sizeof(int)];
        foreach (var bytes in components.Select(component => Encoding.UTF8.GetBytes(component)))
        {
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            stream.Write(length);
            stream.Write(bytes);
        }

        var hash = Convert.ToHexString(SHA256.HashData(stream.ToArray()));
        return $"workflow-activation:v1:{strategy}:{hash}";
    }
}
