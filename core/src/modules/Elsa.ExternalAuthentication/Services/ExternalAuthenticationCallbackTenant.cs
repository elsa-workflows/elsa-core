using System.Security.Cryptography;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Constants;
using Microsoft.AspNetCore.DataProtection;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Protects provider callback state as the tenant id plus a single-use nonce, and restores the full tenant
/// context from that state on the login, logout, and preview callbacks.
/// </summary>
/// <remarks>
/// Finding the tenant is async; pushing it must happen synchronously after that await. An AsyncLocal change
/// made inside an async method does not reach the caller, so <see cref="Resolved.Push"/> is the only way the
/// callback's store work sees the tenant's database.
/// </remarks>
public sealed class ExternalAuthenticationCallbackTenant(
    IDataProtectionProvider dataProtectionProvider,
    ITenantFinder tenantFinder,
    ITenantAccessor tenantAccessor)
{
    /// <summary>
    /// Extra protector lifetime past the transaction lifetime so an expired transaction can still be
    /// unprotected and the store remains the authority on expiry.
    /// </summary>
    public static readonly TimeSpan ProtectorLifetimeMargin = TimeSpan.FromMinutes(5);

    public string Protect(string? tenantId, string nonce, TimeSpan transactionLifetime)
    {
        ArgumentException.ThrowIfNullOrEmpty(nonce);
        if (transactionLifetime < TimeSpan.Zero)
            transactionLifetime = TimeSpan.Zero;
        return CreateProtector().Protect(Normalize(tenantId) + "\n" + nonce, transactionLifetime + ProtectorLifetimeMargin);
    }

    /// <summary>
    /// Unprotects <paramref name="state"/> and resolves the full tenant. Returns <c>null</c> when the state
    /// cannot be unprotected or the tenant cannot be resolved, without changing the ambient tenant, so the
    /// caller does not fall back to the host database.
    /// </summary>
    public async ValueTask<Resolved?> TryResolveAsync(string? state, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(state) || !TryUnprotect(state, out var tenantId, out var nonce))
            return null;

        var tenant = await ResolveAsync(tenantId, cancellationToken);
        return tenant is null ? null : new Resolved(tenantId, nonce, tenant, tenantAccessor);
    }

    public bool MatchesStoredTenantId(string stateTenantId, string? storedTenantId) =>
        string.Equals(Normalize(stateTenantId), Normalize(storedTenantId), StringComparison.Ordinal);

    public static string Normalize(string? tenantId) => string.IsNullOrEmpty(tenantId) ? Tenant.DefaultTenantId : tenantId;

    private ITimeLimitedDataProtector CreateProtector() =>
        dataProtectionProvider.CreateProtector(ExternalAuthenticationDataProtectionPurposes.CallbackState).ToTimeLimitedDataProtector();

    private async Task<Tenant?> ResolveAsync(string tenantId, CancellationToken cancellationToken)
    {
        var found = await tenantFinder.FindByIdAsync(tenantId, cancellationToken);
        if (tenantId == Tenant.DefaultTenantId)
            return found ?? Tenant.Default;

        return found;
    }

    private bool TryUnprotect(string state, out string tenantId, out string nonce)
    {
        tenantId = Tenant.DefaultTenantId;
        nonce = "";
        try
        {
            var payload = CreateProtector().Unprotect(state);
            var separator = payload.IndexOf('\n');
            if (separator < 0 || separator == payload.Length - 1)
                return false;

            tenantId = Normalize(payload[..separator]);
            nonce = payload[(separator + 1)..];
            return nonce.Length > 0;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public sealed class Resolved(string tenantId, string nonce, Tenant tenant, ITenantAccessor tenantAccessor)
    {
        public string TenantId { get; } = tenantId;
        public string Nonce { get; } = nonce;
        public Tenant Tenant { get; } = tenant;

        /// <summary>Pushes the resolved tenant synchronously so store work on the calling stack sees it.</summary>
        public Opened Push() => new(TenantId, Nonce, Tenant, tenantAccessor.PushContext(Tenant));
    }

    public sealed class Opened(string tenantId, string nonce, Tenant tenant, IDisposable context) : IDisposable
    {
        public string TenantId { get; } = tenantId;
        public string Nonce { get; } = nonce;
        public Tenant Tenant { get; } = tenant;
        public void Dispose() => context.Dispose();
    }
}
