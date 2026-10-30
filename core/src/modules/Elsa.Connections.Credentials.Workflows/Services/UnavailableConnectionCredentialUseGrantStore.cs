using Elsa.Connections.Contracts;
using Elsa.Connections.Models;

namespace Elsa.Connections.Credentials.Workflows.Services;

internal sealed class UnavailableConnectionCredentialUseGrantStore : IConnectionCredentialUseGrantStore
{
    public Task<ConnectionCredentialUseGrant?> FindAsync(
        string tenantId, string environmentId, string workflowInstanceId, string logicalBindingId,
        CancellationToken cancellationToken = default) => Task.FromResult<ConnectionCredentialUseGrant?>(null);

    public Task<ConnectionCredentialUseGrant?> TryIssueAsync(
        string tenantId, string environmentId, string workflowInstanceId, string logicalBindingId,
        string connectionId, long bindingRevision, string actorId, DateTimeOffset issuedAt,
        CancellationToken cancellationToken = default) => Task.FromResult<ConnectionCredentialUseGrant?>(null);

    public Task<bool> TryWithdrawAsync(
        string tenantId, string environmentId, string workflowInstanceId, string logicalBindingId,
        long expectedRevision, DateTimeOffset withdrawnAt,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
}
