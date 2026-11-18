using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Identity.Services;

/// <inheritdoc />
[PublicAPI]
public class DefaultApplicationCredentialsValidator : IApplicationCredentialsValidator
{
    private readonly IApiKeyParser _apiKeyParser;
    private readonly IApplicationProvider _applicationProvider;
    private readonly IApplicationStore? _applicationStore;
    private readonly ISecretHasher _secretHasher;
    private readonly ILogger<DefaultApplicationCredentialsValidator> _logger;
    private readonly ITenantAccessor? _tenantAccessor;
    private readonly ITenantService? _tenantService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultApplicationCredentialsValidator"/> class.
    /// </summary>
    public DefaultApplicationCredentialsValidator(IApiKeyParser apiKeyParser, IApplicationProvider applicationProvider, ISecretHasher secretHasher) : this(apiKeyParser, applicationProvider, null, secretHasher, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultApplicationCredentialsValidator"/> class.
    /// </summary>
    public DefaultApplicationCredentialsValidator(IApiKeyParser apiKeyParser, IApplicationProvider applicationProvider, IApplicationStore applicationStore, ISecretHasher secretHasher) : this(apiKeyParser, applicationProvider, applicationStore, secretHasher, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultApplicationCredentialsValidator"/> class.
    /// </summary>
    public DefaultApplicationCredentialsValidator(IApiKeyParser apiKeyParser, IApplicationProvider applicationProvider, IApplicationStore? applicationStore, ISecretHasher secretHasher, ILogger<DefaultApplicationCredentialsValidator>? logger)
    {
        _apiKeyParser = apiKeyParser;
        _applicationProvider = applicationProvider;
        _applicationStore = applicationStore;
        _secretHasher = secretHasher;
        _logger = logger ?? NullLogger<DefaultApplicationCredentialsValidator>.Instance;
    }

    /// <summary>Initializes the host-managed validator with the verified application tenant scope.</summary>
    public DefaultApplicationCredentialsValidator(
        IApiKeyParser apiKeyParser,
        IApplicationProvider applicationProvider,
        IApplicationStore? applicationStore,
        ISecretHasher secretHasher,
        ILogger<DefaultApplicationCredentialsValidator>? logger,
        ITenantAccessor tenantAccessor,
        ITenantService? tenantService = null) : this(apiKeyParser, applicationProvider, applicationStore, secretHasher, logger)
    {
        _tenantAccessor = tenantAccessor;
        _tenantService = tenantService;
    }
    
    /// <inheritdoc />
    public async ValueTask<Application?> ValidateAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(apiKey))
            return null;
        
        var clientId = _apiKeyParser.Parse(apiKey);
        var application = await _applicationProvider.FindByClientIdAsync(clientId, cancellationToken);
        
        if(application == null)
            return null;

        var tenantId = application.TenantId.NormalizeTenantId();
        var ambientTenant = _tenantAccessor?.Tenant;
        if (tenantId == Tenant.AgnosticTenantId || ambientTenant is not null && !string.Equals(ambientTenant.Id, tenantId, StringComparison.Ordinal))
            return null;

        var credentialTenant = ambientTenant;
        if (_tenantAccessor is not null && credentialTenant is null)
        {
            credentialTenant = _tenantService is not null
                ? await _tenantService.FindAsync(tenantId, cancellationToken)
                : tenantId == Tenant.DefaultTenantId ? Tenant.Default : null;
            if (credentialTenant is null)
                return null;
        }

        // Legacy hash upgrades write to the application store and must use the same tenant as validation.
        using var credentialScope = _tenantAccessor?.PushContext(credentialTenant);
        var isValidApiKey = _secretHasher.VerifySecret(apiKey, application.HashedApiKey, application.HashedApiKeySalt, out var needsRehash);

        if (!isValidApiKey)
            return null;

        if (needsRehash && _applicationStore != null)
        {
            var previousHashedApiKey = application.HashedApiKey;
            var previousHashedApiKeySalt = application.HashedApiKeySalt;
            var hashedApiKey = _secretHasher.HashSecret(apiKey);
            application.HashedApiKey = hashedApiKey.EncodeSecret();
            application.HashedApiKeySalt = hashedApiKey.EncodeSalt();
            try
            {
                await _applicationStore.SaveAsync(application, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                application.HashedApiKey = previousHashedApiKey;
                application.HashedApiKeySalt = previousHashedApiKeySalt;
                throw;
            }
            catch (Exception e)
            {
                application.HashedApiKey = previousHashedApiKey;
                application.HashedApiKeySalt = previousHashedApiKeySalt;
                _logger.LogWarning(e, "Failed to save upgraded API key hash for application {ApplicationId}.", application.Id);
            }
        }

        return application;
    }
}
