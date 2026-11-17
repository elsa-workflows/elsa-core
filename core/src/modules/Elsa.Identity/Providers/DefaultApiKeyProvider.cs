using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;
using Elsa;
using Elsa.Common.Multitenancy;
using Elsa.Identity.Constants;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Identity.Providers;

/// <summary>
/// Validates a given API key and returns an instance of <see cref="IApiKey"/> if the key is valid.
/// </summary>
public class DefaultApiKeyProvider : IApiKeyProvider
{
    private readonly IApplicationCredentialsValidator _applicationCredentialsValidator;
    private readonly IRoleProvider _roleProvider;
    private readonly ITenantAccessor? _tenantAccessor;
    private readonly ITenantService? _tenantService;
    private readonly ITenantRequestHints? _tenantHints;
    private readonly string _tenantClaimType = CustomClaimTypes.TenantId;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultApiKeyProvider"/> class.
    /// </summary>
    public DefaultApiKeyProvider(IApplicationCredentialsValidator applicationCredentialsValidator, IRoleProvider roleProvider)
    {
        _applicationCredentialsValidator = applicationCredentialsValidator;
        _roleProvider = roleProvider;
    }

    /// <summary>Initializes API-key authentication with tenant-aware credential and role resolution.</summary>
    public DefaultApiKeyProvider(
        IApplicationCredentialsValidator applicationCredentialsValidator,
        IRoleProvider roleProvider,
        ITenantAccessor tenantAccessor,
        IOptions<IdentityTokenOptions> options,
        ITenantService? tenantService = null,
        ITenantRequestHints? tenantHints = null) : this(applicationCredentialsValidator, roleProvider)
    {
        _tenantAccessor = tenantAccessor;
        _tenantService = tenantService;
        _tenantHints = tenantHints;
        _tenantClaimType = options.Value.TenantIdClaimsType;
    }

    /// <summary>
    /// Gets an instance of <see cref="IApiKey"/>.
    /// </summary>
    /// <param name="key">The API key to validate.</param>
    /// <returns>The API key if a valid key was provided.</returns>
    public async Task<IApiKey?> ProvideAsync(string key)
    {
        var hints = _tenantHints is null ? [] : await _tenantHints.GetAsync();
        if (hints.Count > 1)
            return null;

        var hintedTenantId = hints.SingleOrDefault();
        var ambientTenant = _tenantAccessor?.Tenant;
        if (hintedTenantId is not null && ambientTenant is not null && !string.Equals(hintedTenantId, ambientTenant.Id, StringComparison.Ordinal))
            return null;

        var hintedTenant = hintedTenantId is null ? ambientTenant : await FindTenantAsync(hintedTenantId);
        if (hintedTenantId is not null && hintedTenant is null)
            return null;

        using var credentialScope = _tenantAccessor?.PushContext(hintedTenant);
        var application = await _applicationCredentialsValidator.ValidateAsync(key);
        if (application is null)
            return null;

        var tenantId = application.TenantId.NormalizeTenantId();
        if (tenantId == Tenant.AgnosticTenantId || hintedTenant is not null && !string.Equals(hintedTenant.Id, tenantId, StringComparison.Ordinal))
            return null;
        var tenant = hintedTenant ?? await FindTenantAsync(tenantId);
        if (tenant is null)
            return null;

        using var roleScope = _tenantAccessor?.PushContext(tenant);
        var filter = new RoleFilter { Ids = application.Roles.Distinct().ToList(), TenantId = tenantId };
        var roles = (await _roleProvider.FindManyAsync(filter)).ToList();
        var permissions = roles.SelectMany(x => x.Permissions).Distinct().ToList();
        var claims = new List<Claim>();

        if (permissions.Count == 0)
        {
            claims.Add(new Claim(PermissionNames.ClaimType, PermissionNames.None));
        }
        else
        {
            claims.AddRange(permissions.Select(p => new Claim(PermissionNames.ClaimType, p)));
        }

        claims.Add(new(TenantPrincipalClaims.BoundTenant, tenantId));
        if (_tenantClaimType is not (TenantPrincipalClaims.BoundTenant or TenantPrincipalClaims.HostAdministrator))
            claims.Add(new(_tenantClaimType, tenantId));

        return new ApiKey(key, application.ClientId, claims);
    }

    private async Task<Tenant?> FindTenantAsync(string tenantId)
    {
        if (_tenantService is not null)
            return await _tenantService.FindAsync(tenantId);
        if (_tenantAccessor?.Tenant is { } ambient && string.Equals(ambient.Id, tenantId, StringComparison.Ordinal))
            return ambient;
        return tenantId == Tenant.DefaultTenantId ? Tenant.Default : null;
    }
}
