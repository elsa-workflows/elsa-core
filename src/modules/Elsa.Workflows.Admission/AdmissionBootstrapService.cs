using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Admission;

/// <summary>Explicit inactive, allowlisted provisioning; never arbitrary workflow/instance/bookmark import.</summary>
public sealed class AdmissionBootstrapService(
    IAdmissionStore store,
    IAdmissionDefinitionBootstrapStore definitions,
    IWorkflowDefinitionService definitionService,
    IPayloadSerializer serializer,
    AdmissionHostConfiguration host,
    ITenantAccessor tenant,
    ISystemClock clock,
    IServiceProvider services)
{
    /// <summary>
    /// Only a definite new insert may enter normal publication. Existing unverified content
    /// represents an uncertain prior bootstrap and remains inactive for reconciliation.
    /// </summary>
    public async Task<AdmissionSubscription> ProvisionAsync(AdmissionSubscriptionConfiguration configuration, WorkflowDefinition artifact,
        CancellationToken cancellationToken = default)
    {
        configuration.Validate();
        ValidateScope(configuration);
        await host.ValidateAsync(services, cancellationToken);
        if (artifact.Id != configuration.DefinitionVersionId || artifact.DefinitionId != configuration.DefinitionId ||
            artifact.Version != configuration.DefinitionVersion || artifact.TenantId != configuration.TenantId ||
            artifact.IsPublished || artifact.IsLatest || artifact.MaterializerName != "Json" ||
            AdmissionDefinitionFingerprint.Compute(artifact, serializer) != configuration.DefinitionFingerprint)
        {
            throw new InvalidOperationException("The predefined artifact does not match its allowlisted trusted binding.");
        }
        var graph = await definitionService.MaterializeWorkflowAsync(artifact, cancellationToken);
        host.ValidateGraph(graph);
        await using var exclusive = await definitions.AcquireExclusiveAsync(configuration, cancellationToken);
        var subscription = await store.ProvisionAsync(configuration, cancellationToken);
        if (subscription.Retired || subscription.ReconciliationCode != null)
        {
            throw new InvalidOperationException("Retired or uncertain bootstrap requires explicit reconciliation.");
        }
        try
        {
            var outcome = await definitions.InsertOrVerifyAsync(artifact, configuration.DefinitionFingerprint, cancellationToken);
            var reloaded = await ReloadVerifiedAsync(configuration, false, cancellationToken);
            if (subscription.BootstrapVerified)
            {
                if (!reloaded.IsPublished)
                {
                    throw new InvalidOperationException("The verified bootstrap publication changed.");
                }
                return subscription;
            }
            if (outcome != AdmissionDefinitionInsertOutcome.Inserted || subscription.Active)
            {
                throw new InvalidOperationException("Existing unverified bootstrap cannot repeat publication notifications.");
            }
            // Private built-in publisher, never exposed as an alternate public write facade.
            // Actual public definition/lifecycle mutation services are fail-closed in this host.
            var publisher = ActivatorUtilities.CreateInstance<WorkflowDefinitionPublisher>(services);
            var result = await publisher.PublishAsync(reloaded, cancellationToken);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("The predefined workflow failed publication validation.");
            }
            await ReloadVerifiedAsync(configuration, true, cancellationToken);
            return await store.VerifyBootstrapAsync(configuration.Id, subscription.Revision, configuration.ConfigurationFingerprint, cancellationToken)
                ?? throw new InvalidOperationException("Bootstrap verification lost its ledger revision.");
        }
        catch
        {
            await WithdrawAfterFailureAsync(configuration.Id, "bootstrap-outcome-unknown");
            throw;
        }
    }

    /// <summary>Separate explicit activation; there are no numeric or grant defaults and no automatic start.</summary>
    public async Task<AdmissionSubscription?> ActivateAsync(string subscriptionId, long revision, CancellationToken cancellationToken = default)
    {
        await host.ValidateAsync(services, cancellationToken);
        var subscription = await store.FindSubscriptionAsync(subscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("The trusted subscription is missing.");
        ValidateScope(subscription.Configuration);
        await using var exclusive = await definitions.AcquireExclusiveAsync(subscription.Configuration, cancellationToken);
        await ReloadVerifiedAsync(subscription.Configuration, true, cancellationToken);
        return await store.ActivateAsync(subscriptionId, revision, clock.UtcNow, cancellationToken);
    }

    /// <summary>Withdrawal linearizes before the supported actual retract operation. Failure never reactivates the subscription.</summary>
    public async Task WithdrawAndRetractAsync(string subscriptionId, long revision, bool retire = false, CancellationToken cancellationToken = default)
    {
        await host.ValidateAsync(services, cancellationToken);
        var subscription = await store.FindSubscriptionAsync(subscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("The trusted subscription is missing.");
        ValidateScope(subscription.Configuration);
        await using var exclusive = await definitions.AcquireExclusiveAsync(subscription.Configuration, cancellationToken);
        var withdrawn = await store.WithdrawAsync(subscriptionId, revision, retire, null, cancellationToken)
            ?? throw new InvalidOperationException("Withdrawal did not establish its authoritative ledger boundary.");
        try
        {
            var publisher = ActivatorUtilities.CreateInstance<WorkflowDefinitionPublisher>(services);
            await publisher.RetractAsync(withdrawn.Configuration.DefinitionVersionId, cancellationToken);
        }
        catch
        {
            await WithdrawAfterFailureAsync(subscriptionId, "withdrawal-external-mutation-incomplete");
            throw;
        }
    }

    private async Task<WorkflowDefinition> ReloadVerifiedAsync(AdmissionSubscriptionConfiguration configuration, bool requirePublished, CancellationToken cancellationToken)
    {
        var definition = await definitionService.FindWorkflowDefinitionAsync(configuration.DefinitionVersionId, cancellationToken)
            ?? throw new InvalidOperationException("The predefined bootstrap definition is missing.");
        if (definition.TenantId != configuration.TenantId || definition.DefinitionId != configuration.DefinitionId || definition.Version != configuration.DefinitionVersion ||
            (requirePublished && !definition.IsPublished) || AdmissionDefinitionFingerprint.Compute(definition, serializer) != configuration.DefinitionFingerprint)
        {
            throw new InvalidOperationException("The stored bootstrap content changed.");
        }
        return definition;
    }
    private void ValidateScope(AdmissionSubscriptionConfiguration configuration)
    {
        host.ValidateSubscription(configuration);
        if (configuration.TenantId != host.TenantId || configuration.TenantId != tenant.TenantId || configuration.EnvironmentId != host.EnvironmentId)
        {
            throw new InvalidOperationException("Bootstrap cannot select a different trusted host scope.");
        }
    }
    private async Task WithdrawAfterFailureAsync(string id, string code)
    {
        try
        {
            var current = await store.FindSubscriptionAsync(id, CancellationToken.None);
            if (current != null)
            {
                await store.WithdrawAsync(id, current.Revision, current.Retired, code, CancellationToken.None);
            }
        }
        catch
        {
            // Preserve the original failure; the unresolved inactive/unverified boundary
            // cannot be turned into successful publication/activation by this coordinator.
        }
    }
}
