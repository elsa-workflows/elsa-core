using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Notifications;
using Elsa.ExternalAuthentication.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>Executes an administrator-bound adapter flow without resolving links, provisioning users, sessions, or credentials.</summary>
public sealed class PreviewSignInService(
    IdentityProviderConnectionManagementService management,
    IExternalAuthenticationAdapterRegistry adapters,
    IEnumerable<ISecretBindingResolver> secretBindingResolvers,
    IEnumerable<IUnlinkedIdentityPolicy> policies,
    IExternalIdentityProvisioner provisioner,
    IPermissionGrantResolver permissionGrants,
    IExternalAuthenticationStateStore stateStore,
    IPreviewResultStore results,
    IExternalAuthenticationHandleHasher handles,
    IDataProtectionProvider dataProtection,
    ISystemClock clock,
    IOptions<ExternalAuthenticationOptions> options,
    ExternalAuthenticationSecurityNotifier notifier)
{
    private const string StartPurpose = "PreviewStart";
    private readonly IReadOnlyDictionary<string, ISecretBindingResolver> _resolvers = secretBindingResolvers.ToDictionary(x => x.Type, StringComparer.Ordinal);
    private readonly IReadOnlyDictionary<string, IUnlinkedIdentityPolicy> _policies = policies.ToDictionary(x => x.Type, StringComparer.Ordinal);

    public async ValueTask<PreviewInitiationResult> InitiateAsync(string connectionId, long expectedRevision, string tenantId, ClaimsPrincipal administrator, CancellationToken cancellationToken = default)
    {
        var lookup = await management.FindAsync(connectionId, tenantId, cancellationToken);
        if (lookup is not ManagementConnectionLookupResult.Found(var connection)) return new PreviewInitiationResult.NotFound();
        if (connection.Connection.Revision != expectedRevision) return new PreviewInitiationResult.PreconditionFailed(connection.Connection.Revision);
        var adminId = ActorId(administrator);
        if (adminId is null) return new PreviewInitiationResult.Forbidden();
        var handle = Opaque();
        var transaction = new BrokerTransaction
        {
            HandleHash = handles.Hash(handle), Purpose = BrokerTransactionPurpose.Preview, ClientId = adminId,
            CallbackUri = new Uri($"/external-authentication/previews/{Uri.EscapeDataString(handle)}/authorize", UriKind.Relative),
            ReturnPath = "/", TenantId = tenantId, ConnectionId = connection.Connection.Id,
            ConnectionMaterialRevision = connection.Connection.MaterialRevision, PkceChallenge = string.Empty,
            ExpiresAt = clock.UtcNow.Add(options.Value.Lifetimes.PreviewLifetime)
        };
        await stateStore.PutAsync(StartPurpose, transaction.HandleHash, transaction, transaction.ExpiresAt, cancellationToken);
        return new PreviewInitiationResult.Started(handle, transaction.ExpiresAt);
    }

    public async ValueTask<PreviewAuthorizeResult> AuthorizeAsync(string previewHandle, string tenantId, ClaimsPrincipal administrator, CancellationToken cancellationToken = default)
    {
        var adminId = ActorId(administrator);
        var taken = await stateStore.TryTakeAsync<BrokerTransaction>(StartPurpose, handles.Hash(previewHandle), cancellationToken);
        if (adminId is null || taken is not TakeResult<BrokerTransaction>.Taken { Value: var transaction } || !string.Equals(transaction.ClientId, adminId, StringComparison.Ordinal) || !string.Equals(transaction.TenantId, tenantId, StringComparison.Ordinal))
            return new PreviewAuthorizeResult.Invalid();
        var lookup = await management.FindAsync(transaction.ConnectionId!, tenantId, cancellationToken);
        if (lookup is not ManagementConnectionLookupResult.Found(var connection) || !string.Equals(connection.Connection.MaterialRevision, transaction.ConnectionMaterialRevision, StringComparison.Ordinal) || !adapters.TryGet(connection.Connection.AdapterType, out var adapter))
            return new PreviewAuthorizeResult.Invalid();

        var state = Opaque();
        transaction.HandleHash = handles.Hash(state);
        transaction.ClientState = previewHandle;
        var secrets = await ResolveSecretsAsync(connection.Connection.SecretBindings, cancellationToken);
        try
        {
            transaction.SecretGenerationFingerprint = SecretFingerprint(secrets);
            var request = await adapter.CreateAuthorizationRequestAsync(new ExternalAuthorizationContext(connection, secrets, transaction, state, clock), cancellationToken);
            transaction.ProtectedPayload = dataProtection.CreateProtector("Elsa.ExternalAuthentication.AdapterPayload.v1").Protect(request.ProtectedAdapterState);
            await stateStore.PutAsync(BrokerTransactionPurpose.Preview.ToString(), transaction.HandleHash, transaction, transaction.ExpiresAt, cancellationToken);
            return new PreviewAuthorizeResult.Redirect(request.NavigationUri);
        }
        finally { Dispose(secrets); }
    }

    public async ValueTask<PreviewCallbackResult> CompleteAsync(string connectionId, string state, IReadOnlyDictionary<string, IReadOnlyCollection<string>> parameters, CancellationToken cancellationToken = default)
    {
        var taken = await stateStore.TryTakeAsync<BrokerTransaction>(BrokerTransactionPurpose.Preview.ToString(), handles.Hash(state), cancellationToken);
        if (taken is not TakeResult<BrokerTransaction>.Taken { Value: var transaction } || !string.Equals(transaction.ConnectionId, connectionId, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(transaction.ClientState))
            return new PreviewCallbackResult.Invalid();
        var lookup = await management.FindAsync(connectionId, transaction.TenantId, cancellationToken);
        if (lookup is not ManagementConnectionLookupResult.Found(var connection) || !string.Equals(connection.Connection.MaterialRevision, transaction.ConnectionMaterialRevision, StringComparison.Ordinal) || !adapters.TryGet(connection.Connection.AdapterType, out var adapter))
            return new PreviewCallbackResult.Invalid();
        var secrets = await ResolveSecretsAsync(connection.Connection.SecretBindings, cancellationToken);
        try
        {
            if (!string.Equals(transaction.SecretGenerationFingerprint, SecretFingerprint(secrets), StringComparison.Ordinal)) return new PreviewCallbackResult.Invalid();
            var protectedPayload = transaction.ProtectedPayload;
            transaction.ProtectedPayload = dataProtection.CreateProtector("Elsa.ExternalAuthentication.AdapterPayload.v1").Unprotect(protectedPayload);
            ExternalAuthenticationResult authentication;
            try { authentication = await adapter.AuthenticateCallbackAsync(new ExternalCallbackContext(connection, secrets, transaction, state, parameters, clock), cancellationToken); }
            finally { transaction.ProtectedPayload = protectedPayload; }
            var existingLink = await provisioner.FindLinkAsync(transaction.TenantId, connection.Connection.Id, authentication.Identity, cancellationToken);
            var decision = existingLink is null ? await DescribePolicyAsync(connection, authentication, cancellationToken) : "would_sign_in_existing_link";
            var grants = existingLink is null
                ? new PermissionGrantResult([], [new PermissionGrantWarning("user_resolution_required", "Permission grants require an existing Elsa user link.")])
                : await permissionGrants.ResolveAsync(new PermissionGrantResolutionContext(transaction.TenantId, existingLink.UserId, connection, authentication.Identity, authentication.ProjectedClaims), cancellationToken);
            var result = new PreviewResult(handles.Hash(transaction.ClientState), transaction.ClientId, transaction.TenantId, connection.Connection.Id, connection.Connection.MaterialRevision,
                authentication.Identity.Issuer, Mask(authentication.Identity.Subject), ExternalAuthenticationRedactor.RedactProjectedClaims(authentication.ProjectedClaims, connection.Connection.ClaimProjection.RedactedClaimTypes), decision, grants.Grants, [.. authentication.Warnings, .. grants.Warnings.Select(x => x.Message)], transaction.ExpiresAt, null);
            await results.SaveAsync(result, cancellationToken);
            await notifier.PublishAsync(new IdentityProviderConnectionPreviewed(ExternalAuthenticationSecurityNotifier.Context(transaction.ClientId, transaction.TenantId, connection.Connection.Id, existingLink?.UserId, SecurityEventOutcome.Succeeded, "Identity provider sign-in preview completed."), connection.Connection.MaterialRevision), cancellationToken);
            return new PreviewCallbackResult.Completed();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new PreviewCallbackResult.Invalid(); }
        finally { Dispose(secrets); }
    }

    public async ValueTask<TakeResult<PreviewResult>> TakeResultAsync(string previewHandle, string tenantId, ClaimsPrincipal administrator, CancellationToken cancellationToken = default)
    {
        var adminId = ActorId(administrator);
        if (adminId is null) return new TakeResult<PreviewResult>.NotFound();
        var result = await results.TryTakeAsync(handles.Hash(previewHandle), adminId, cancellationToken);
        return result is TakeResult<PreviewResult>.Taken { Value: var preview } && !string.Equals(preview.TenantId, tenantId, StringComparison.Ordinal) ? new TakeResult<PreviewResult>.NotFound() : result;
    }

    private async ValueTask<string> DescribePolicyAsync(EffectiveIdentityProviderConnection connection, ExternalAuthenticationResult authentication, CancellationToken cancellationToken)
    {
        var selection = connection.Connection.UnlinkedPolicy ?? new PolicySelection(options.Value.UnlinkedIdentityPolicy.DefaultType, 1, default);
        if (!_policies.TryGetValue(selection.Type, out var policy)) return "unlinked_policy_unavailable";
        var decision = await policy.EvaluateAsync(new UnlinkedIdentityContext(connection.Connection.TenantId, connection, authentication.Identity, authentication.ProjectedClaims, selection.Settings), cancellationToken);
        return decision switch
        {
            UnlinkedIdentityDecision.CreateUser => "would_create_user_and_link",
            UnlinkedIdentityDecision.LinkExistingUser => "would_link_existing_user",
            UnlinkedIdentityDecision.Reject => "would_reject_unlinked_identity",
            _ => "unlinked_policy_unknown"
        };
    }

    private async ValueTask<IReadOnlyDictionary<string, ResolvedSecretBinding>> ResolveSecretsAsync(IDictionary<string, SecretBinding> bindings, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, ResolvedSecretBinding>(StringComparer.Ordinal);
        try { foreach (var (name, binding) in bindings) values[name] = await (_resolvers.TryGetValue(binding.ResolverType, out var resolver) ? resolver : throw new InvalidOperationException()).ResolveAsync(binding, cancellationToken); return values; }
        catch { Dispose(values); throw; }
    }
    private static string? ActorId(ClaimsPrincipal user) => user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
    private static string Opaque() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string Mask(string subject) => subject.Length <= 4 ? "****" : subject[..2] + "***" + subject[^2..];
    private static string SecretFingerprint(IReadOnlyDictionary<string, ResolvedSecretBinding> values) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", values.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{x.Key}:{x.Value.GenerationFingerprint}")))));
    private static void Dispose(IReadOnlyDictionary<string, ResolvedSecretBinding> values) { foreach (var value in values.Values) value.Value.Dispose(); }
}

public abstract record PreviewInitiationResult { private PreviewInitiationResult() { } public sealed record Started(string Handle, DateTimeOffset ExpiresAt) : PreviewInitiationResult; public sealed record NotFound : PreviewInitiationResult; public sealed record PreconditionFailed(long CurrentRevision) : PreviewInitiationResult; public sealed record Forbidden : PreviewInitiationResult; }
public abstract record PreviewAuthorizeResult { private PreviewAuthorizeResult() { } public sealed record Redirect(Uri NavigationUri) : PreviewAuthorizeResult; public sealed record Invalid : PreviewAuthorizeResult; }
public abstract record PreviewCallbackResult { private PreviewCallbackResult() { } public sealed record Completed : PreviewCallbackResult; public sealed record Invalid : PreviewCallbackResult; }
