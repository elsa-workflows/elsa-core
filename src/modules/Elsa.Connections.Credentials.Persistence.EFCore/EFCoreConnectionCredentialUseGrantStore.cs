using System.Data;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Credentials.Persistence.EFCore;

/// <summary>Persists narrowly scoped use grants beside the binding and connection state they authorize.</summary>
public sealed class EFCoreConnectionCredentialUseGrantStore(
    IDbContextFactory<ConnectionsElsaDbContext> dbContextFactory,
    IConnectionCredentialBindingConflictClassifier conflictClassifier) : IConnectionCredentialUseGrantStore
{
    public async Task<ConnectionCredentialUseGrant?> FindAsync(
        string tenantId, string environmentId, string workflowInstanceId, string logicalBindingId,
        CancellationToken cancellationToken = default)
    {
        if (!ValidTenant(tenantId) || !Valid(environmentId) || !Valid(workflowInstanceId) || !Valid(logicalBindingId))
        {
            return null;
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await GrantQuery(db, tenantId, environmentId, workflowInstanceId, logicalBindingId)
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ConnectionCredentialUseGrant?> TryIssueAsync(
        string tenantId, string environmentId, string workflowInstanceId, string logicalBindingId,
        string connectionId, long bindingRevision, string actorId, DateTimeOffset issuedAt,
        CancellationToken cancellationToken = default)
    {
        if (!ValidTenant(tenantId) || !Valid(environmentId) || !Valid(workflowInstanceId) ||
            !Valid(logicalBindingId) || !Valid(connectionId) || !Valid(actorId) ||
            bindingRevision < 1 || issuedAt.Offset != TimeSpan.Zero)
        {
            return null;
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var binding = await db.CredentialBindings.AsNoTracking().SingleOrDefaultAsync(x =>
            x.TenantId == tenantId && x.EnvironmentId == environmentId && x.LogicalBindingId == logicalBindingId,
            cancellationToken);
        if (binding is null || binding.Revision != bindingRevision || binding.ConnectionId != connectionId ||
            !await db.Connections.AnyAsync(x => x.Id == connectionId && x.TenantId == tenantId &&
                x.EnvironmentId == environmentId && x.Status == ConnectionStatus.Active, cancellationToken) ||
            await GrantQuery(db, tenantId, environmentId, workflowInstanceId, logicalBindingId).AnyAsync(cancellationToken))
        {
            return null;
        }

        var grant = new ConnectionCredentialUseGrant
        {
            TenantId = tenantId,
            EnvironmentId = environmentId,
            WorkflowInstanceId = workflowInstanceId,
            LogicalBindingId = logicalBindingId,
            ConnectionId = connectionId,
            BindingRevision = bindingRevision,
            Revision = 1,
            IsActive = true,
            IssuedByActorId = actorId,
            IssuedAt = issuedAt
        };
        db.CredentialUseGrants.Add(grant);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            !cancellationToken.IsCancellationRequested && conflictClassifier.IsDuplicateBindingKey(exception))
        {
            return null;
        }

        return grant;
    }

    public async Task<bool> TryWithdrawAsync(
        string tenantId, string environmentId, string workflowInstanceId, string logicalBindingId,
        long expectedRevision, DateTimeOffset withdrawnAt,
        CancellationToken cancellationToken = default)
    {
        if (!ValidTenant(tenantId) || !Valid(environmentId) || !Valid(workflowInstanceId) ||
            !Valid(logicalBindingId) || expectedRevision < 1 || expectedRevision == long.MaxValue ||
            withdrawnAt.Offset != TimeSpan.Zero)
        {
            return false;
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var changed = await GrantQuery(db, tenantId, environmentId, workflowInstanceId, logicalBindingId)
            .Where(x => x.Revision == expectedRevision && x.IsActive)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.Revision, x => x.Revision + 1)
                .SetProperty(x => x.WithdrawnAt, withdrawnAt), cancellationToken);
        return changed == 1;
    }

    private static IQueryable<ConnectionCredentialUseGrant> GrantQuery(
        ConnectionsElsaDbContext db, string tenantId, string environmentId,
        string workflowInstanceId, string logicalBindingId) =>
        db.CredentialUseGrants.Where(x => x.TenantId == tenantId && x.EnvironmentId == environmentId &&
            x.WorkflowInstanceId == workflowInstanceId && x.LogicalBindingId == logicalBindingId);

    private static bool Valid(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 200;

    private static bool ValidTenant(string value) => Valid(value) &&
        value != Tenant.DefaultTenantId && value != Tenant.AgnosticTenantId;
}
