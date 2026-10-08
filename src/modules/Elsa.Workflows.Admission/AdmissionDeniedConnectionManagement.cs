using System.Security.Claims;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;

namespace Elsa.Workflows.Admission;

// Reuses the existing connection/binding/grant stores and workflow resolver. No second
// credential store or new delegation policy. Generic lifecycle mutation is unsupported;
// this leaf also never permits a credential/provider call via the background facade.
internal sealed class AdmissionDeniedConnectionManagement : IConnectionLifecycleService, IStaticApiKeyLifecycleService,
    IConnectionLifecycleRecoveryService, IConnectionBackgroundUseService
{
    private static InvalidOperationException Denied() => new("Connection lifecycle operations require separate supported withdrawal and activation authority.");
    public Task<ConnectionLifecycleResult> ConnectAsync(ClaimsPrincipal principal, ConnectConnectionRequest request, CancellationToken cancellationToken = default) => throw Denied();
    public Task<ConnectionAccessCredential> ResolveForUseAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default) => throw Denied();
    public Task<ConnectionLifecycleResult> RefreshAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default) => throw Denied();
    public Task<ConnectionOffboardingOperationResult> DisconnectAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default) => throw Denied();
    public Task<ConnectionOffboardingOperationResult> RequestTokenRevocationAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, string generationId, CancellationToken cancellationToken = default) => throw Denied();
    public Task<ConnectionOffboardingOperationResult> RequestInstallationUninstallAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default) => throw Denied();
    public Task<ConnectionLifecycleResult> CleanupGenerationAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, string generationId, CancellationToken cancellationToken = default) => throw Denied();
    public Task<ConnectionLifecycleResult> ConnectApiKeyAsync(ClaimsPrincipal principal, ConnectApiKeyConnectionRequest request, CancellationToken cancellationToken = default) => throw Denied();
    public Task<ConnectionLifecycleResult> ReplaceApiKeyAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, long expectedRevision, string apiKey, CancellationToken cancellationToken = default) => throw Denied();
    public Task<ConnectionLifecycleResult> ReconcileAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default) => throw Denied();
    public Task<ConnectionOffboardingOperationResult> ReconcileOffboardingAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default) => throw Denied();
    public Task<ConnectionLifecycleResult> RefreshAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default) => throw Denied();
    public Task<ConnectionLifecycleResult> CleanupGenerationAsync(string tenantId, string environmentId, string connectionId, string generationId, CancellationToken cancellationToken = default) => throw Denied();
    public Task<ConnectionAccessCredential> ResolveForUseAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default) => throw Denied();
}
