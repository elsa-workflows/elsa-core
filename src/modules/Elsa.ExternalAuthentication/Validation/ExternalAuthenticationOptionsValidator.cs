using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Validation;

/// <summary>
/// Validates the deployment-owned External Authentication configuration before it is used by the broker.
/// </summary>
public sealed class ExternalAuthenticationOptionsValidator(
    IOptions<ExternalAuthenticationExtensionOptions> extensionOptions) : IValidateOptions<ExternalAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, ExternalAuthenticationOptions options)
    {
        var failures = new List<string>();
        var registrations = extensionOptions.Value.Registrations;
        var installedAdapterTypes = ValidateExtensionTypes(registrations.Where(x => x.Kind == ExternalAuthenticationExtensionKind.Adapter).Select(x => x.Type), "adapter", failures);
        var installedPolicyTypes = ValidateExtensionTypes(registrations.Where(x => x.Kind == ExternalAuthenticationExtensionKind.UnlinkedIdentityPolicy).Select(x => x.Type), "unlinked identity policy", failures);
        var installedGrantSourceTypes = ValidateExtensionTypes(registrations.Where(x => x.Kind == ExternalAuthenticationExtensionKind.PermissionGrantSource).Select(x => x.Type), "permission grant source", failures);

        ValidateAllowedTypes(options.AllowedAdapterTypes, installedAdapterTypes, "adapter", failures);
        ValidateAllowedTypes(options.AllowedUnlinkedIdentityPolicyTypes, installedPolicyTypes, "unlinked identity policy", failures);
        ValidateAllowedTypes(options.AllowedPermissionGrantSourceTypes, installedGrantSourceTypes, "permission grant source", failures);
        ValidateClients(options, failures);
        ValidateConfigurationConnections(options, installedAdapterTypes, installedPolicyTypes, installedGrantSourceTypes, failures);
        ValidateRateLimits(options.RateLimits, failures);
        ValidateProviderEgress(options.ProviderEgress, failures);
        ValidateHandleHashing(options.HandleHashing, failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateHandleHashing(ExternalAuthenticationHandleHashingOptions? handleHashing, ICollection<string> failures)
    {
        if (handleHashing is null)
        {
            failures.Add("External Authentication handle-hashing settings are required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(handleHashing.SharedKeyBase64))
            return;

        byte[]? key = null;
        try
        {
            key = Convert.FromBase64String(handleHashing.SharedKeyBase64);
            if (key.Length < 32)
                failures.Add("External Authentication HandleHashing:SharedKeyBase64 must contain at least 32 bytes.");
        }
        catch (FormatException)
        {
            failures.Add("External Authentication HandleHashing:SharedKeyBase64 must be valid base64.");
        }
        finally
        {
            if (key is not null)
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
        }
    }

    private static void ValidateRateLimits(ExternalAuthenticationRateLimitOptions rateLimits, ICollection<string> failures)
    {
        foreach (var (name, rule) in new (string Name, RateLimitRule? Rule)[]
        {
            (nameof(rateLimits.Discovery), rateLimits.Discovery),
            (nameof(rateLimits.ExternalInitiation), rateLimits.ExternalInitiation),
            (nameof(rateLimits.LocalInitiation), rateLimits.LocalInitiation),
            (nameof(rateLimits.ProviderCallback), rateLimits.ProviderCallback),
            (nameof(rateLimits.TokenExchange), rateLimits.TokenExchange)
        })
        {
            if (rule is null || rule.PermitLimit <= 0 || rule.Window <= TimeSpan.Zero)
                failures.Add($"External Authentication rate limit '{name}' must have a positive permit limit and window.");
        }
    }

    private static void ValidateProviderEgress(ProviderEgressOptions? providerEgress, ICollection<string> failures)
    {
        if (providerEgress is null)
        {
            failures.Add("External Authentication provider egress settings are required.");
            return;
        }

        if (providerEgress.MaximumRedirects < 0)
            failures.Add("External Authentication provider egress maximum redirects must not be negative.");
        if (providerEgress.ConnectTimeout <= TimeSpan.Zero || providerEgress.RequestTimeout <= TimeSpan.Zero)
            failures.Add("External Authentication provider egress timeouts must be positive.");
        if (providerEgress.MaximumDiscoveryResponseBytes <= 0 || providerEgress.MaximumTokenResponseBytes <= 0 || providerEgress.MaximumUserInfoResponseBytes <= 0)
            failures.Add("External Authentication provider egress response-size limits must be positive.");

        foreach (var host in providerEgress.AllowedHosts ?? [])
        {
            if (string.IsNullOrWhiteSpace(host) || host.Contains('*') || Uri.CheckHostName(host.TrimEnd('.')) == UriHostNameType.Unknown)
                failures.Add($"External Authentication provider egress allowed host '{host}' is invalid.");
        }

        if (providerEgress.ProxyUri is { } proxyUri &&
            (!proxyUri.IsAbsoluteUri ||
             !string.Equals(proxyUri.Scheme, "http", StringComparison.OrdinalIgnoreCase) && !string.Equals(proxyUri.Scheme, "https", StringComparison.OrdinalIgnoreCase) ||
             !string.IsNullOrEmpty(proxyUri.UserInfo) ||
             !string.IsNullOrEmpty(proxyUri.Fragment) ||
             proxyUri.HostNameType == UriHostNameType.Unknown))
            failures.Add("External Authentication provider egress proxy URI must be an absolute HTTP(S) URI without credentials or a fragment.");
    }

    private static HashSet<string> ValidateExtensionTypes(IEnumerable<string> types, string kind, ICollection<string> failures)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        foreach (var type in types)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                failures.Add($"Installed {kind} types must not be empty.");
                continue;
            }

            if (!result.Add(type))
                failures.Add($"The installed {kind} type '{type}' is registered more than once.");
        }

        return result;
    }

    private static void ValidateAllowedTypes(IEnumerable<string>? allowedTypes, IReadOnlySet<string> installedTypes, string kind, ICollection<string> failures)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var type in allowedTypes ?? [])
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                failures.Add($"Allowed {kind} types must not be empty.");
                continue;
            }

            if (!seen.Add(type))
                failures.Add($"The allowed {kind} type '{type}' is configured more than once.");

            if (!installedTypes.Contains(type))
                failures.Add($"The allowed {kind} type '{type}' is not installed.");
        }
    }

    private static void ValidateClients(ExternalAuthenticationOptions options, ICollection<string> failures)
    {
        var clientIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var client in options.Clients ?? [])
        {
            if (string.IsNullOrWhiteSpace(client.ClientId))
                failures.Add("Authentication client identifiers must not be empty.");
            else if (!clientIds.Add(client.ClientId))
                failures.Add($"The authentication client identifier '{client.ClientId}' is configured more than once.");

            var callbackUris = client.CallbackUris ?? new HashSet<Uri>();
            var logoutCallbackUris = client.LogoutCallbackUris ?? new HashSet<Uri>();
            var allowedOrigins = client.AllowedOrigins ?? new HashSet<string>();
            var allowedReturnPathPrefixes = client.AllowedReturnPathPrefixes ?? new HashSet<string>();

            if (callbackUris.Count == 0)
                failures.Add($"Authentication client '{client.ClientId}' must register at least one callback URI.");

            ValidateUris(client.ClientId, callbackUris, "callback", options.Redirects.AllowDevelopmentLoopbackCallbacks, failures);
            ValidateUris(client.ClientId, logoutCallbackUris, "logout callback", options.Redirects.AllowDevelopmentLoopbackCallbacks, failures);
            ValidateOrigins(client.ClientId, allowedOrigins, options.Redirects.AllowDevelopmentLoopbackCallbacks, failures);
            ValidateReturnPathPrefixes(client.ClientId, allowedReturnPathPrefixes, failures);

            if (client.ClientType == AuthenticationClientType.Public)
            {
                if (client.SecretBinding is not null)
                    failures.Add($"Public authentication client '{client.ClientId}' must not define a client secret binding.");

                if (allowedOrigins.Count == 0)
                    failures.Add($"Public authentication client '{client.ClientId}' must register at least one allowed origin.");

                var originSet = allowedOrigins.ToHashSet(StringComparer.Ordinal);
                foreach (var callbackUri in callbackUris)
                {
                    if (callbackUri.IsAbsoluteUri && !string.IsNullOrWhiteSpace(callbackUri.Host) && !originSet.Contains(callbackUri.GetLeftPart(UriPartial.Authority)))
                        failures.Add($"Public authentication client '{client.ClientId}' callback URI '{callbackUri}' does not belong to an allowed origin.");
                }
            }
            else if (client.SecretBinding is null)
                failures.Add($"Confidential authentication client '{client.ClientId}' must define a client secret binding.");
        }
    }

    private static void ValidateUris(string clientId, IEnumerable<Uri> uris, string registrationKind, bool allowDevelopmentLoopback, ICollection<string> failures)
    {
        var registeredUris = new HashSet<string>(StringComparer.Ordinal);

        foreach (var uri in uris)
        {
            if (!IsExactClientUri(uri, allowDevelopmentLoopback))
            {
                failures.Add($"Authentication client '{clientId}' has an invalid {registrationKind} URI '{uri}'. Registrations must be absolute HTTPS URIs without a fragment, wildcard, or user info.");
                continue;
            }

            if (!registeredUris.Add(uri.AbsoluteUri))
                failures.Add($"Authentication client '{clientId}' registers {registrationKind} URI '{uri}' more than once.");
        }
    }

    private static void ValidateOrigins(string clientId, IEnumerable<string> origins, bool allowDevelopmentLoopback, ICollection<string> failures)
    {
        var registeredOrigins = new HashSet<string>(StringComparer.Ordinal);

        foreach (var origin in origins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                !IsExactClientUri(uri, allowDevelopmentLoopback) ||
                uri.AbsolutePath != "/" ||
                uri.Query.Length != 0)
            {
                failures.Add($"Authentication client '{clientId}' has invalid allowed origin '{origin}'. Origins must be exact scheme, host, and optional port registrations.");
                continue;
            }

            var normalizedOrigin = uri.GetLeftPart(UriPartial.Authority);
            if (!string.Equals(origin, normalizedOrigin, StringComparison.Ordinal))
                failures.Add($"Authentication client '{clientId}' allowed origin '{origin}' must not contain a path, query, or trailing slash.");

            if (!registeredOrigins.Add(normalizedOrigin))
                failures.Add($"Authentication client '{clientId}' registers allowed origin '{origin}' more than once.");
        }
    }

    private static void ValidateReturnPathPrefixes(string clientId, IReadOnlySet<string> prefixes, ICollection<string> failures)
    {
        if (prefixes.Count == 0)
        {
            failures.Add($"Authentication client '{clientId}' must register at least one allowed return-path prefix.");
            return;
        }

        var registeredPrefixes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var prefix in prefixes)
        {
            if (!ClientReturnPathValidator.TryValidateForClient(prefix, new HashSet<string>(StringComparer.Ordinal) { prefix }, out _))
            {
                failures.Add($"Authentication client '{clientId}' has invalid allowed return-path prefix '{prefix}'.");
                continue;
            }

            if (!registeredPrefixes.Add(prefix))
                failures.Add($"Authentication client '{clientId}' registers allowed return-path prefix '{prefix}' more than once.");
        }
    }

    private static bool IsExactClientUri(Uri uri, bool allowDevelopmentLoopback)
    {
        if (!uri.IsAbsoluteUri || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo) || uri.Host.Contains('*'))
            return false;

        return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            allowDevelopmentLoopback && uri.IsLoopback && string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateConfigurationConnections(
        ExternalAuthenticationOptions options,
        IReadOnlySet<string> installedAdapterTypes,
        IReadOnlySet<string> installedPolicyTypes,
        IReadOnlySet<string> installedGrantSourceTypes,
        ICollection<string> failures)
    {
        var connectionKeys = new HashSet<(string TenantId, string Key)>();
        var inheritedHostKeys = new HashSet<string>(StringComparer.Ordinal);
        var configuredConnectionIds = new HashSet<string>(StringComparer.Ordinal);
        var configuredDefaultScopes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var connection in options.ConfigurationConnections ?? [])
        {
            var tenantId = connection.TenantId ?? string.Empty;
            var key = connection.Key?.Trim().ToLowerInvariant() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(tenantId) && connection.TenantId is not "")
                failures.Add("Configuration connection tenant identifiers must not contain whitespace.");

            if (string.IsNullOrWhiteSpace(key))
                failures.Add("Configuration connection keys must not be empty.");
            else if (!connectionKeys.Add((tenantId, key)))
                failures.Add($"Configuration connection key '{connection.Key}' is configured more than once for tenant '{tenantId}'.");

            if (tenantId == ConnectionScope.HostTenantId)
                inheritedHostKeys.Add(key);
            else if (!string.IsNullOrWhiteSpace(key) && inheritedHostKeys.Contains(key))
                failures.Add($"Configuration connection key '{connection.Key}' collides with an inherited host-wide connection.");

            if (!string.IsNullOrWhiteSpace(connection.Id) && !configuredConnectionIds.Add(connection.Id))
                failures.Add($"Configuration connection ID '{connection.Id}' is configured more than once.");

            if (connection.IsDefault && !configuredDefaultScopes.Add(tenantId))
                failures.Add($"Configuration connections define more than one automatic default for scope '{tenantId}'.");

            if (string.IsNullOrWhiteSpace(connection.AdapterType) || !installedAdapterTypes.Contains(connection.AdapterType))
                failures.Add($"Configuration connection '{connection.Key}' selects adapter type '{connection.AdapterType}', which is not installed.");
            else if (options.AllowedAdapterTypes.Count > 0 && !options.AllowedAdapterTypes.Contains(connection.AdapterType, StringComparer.Ordinal))
                failures.Add($"Configuration connection '{connection.Key}' selects adapter type '{connection.AdapterType}', which is not allowed.");

            if (connection.AdapterSettingsVersion <= 0)
                failures.Add($"Configuration connection '{connection.Key}' must use a positive adapter settings version.");

            if (string.IsNullOrWhiteSpace(connection.DisplayName))
                failures.Add($"Configuration connection '{connection.Key}' must define a display name.");

            ValidatePolicy(connection, options, installedPolicyTypes, failures);
            ValidateGrantSources(connection, options, installedGrantSourceTypes, failures);
        }

        foreach (var hostKey in inheritedHostKeys)
        {
            if (connectionKeys.Any(x => x.TenantId != ConnectionScope.HostTenantId && x.Key == hostKey))
                failures.Add($"Configuration connection key '{hostKey}' collides with an inherited host-wide connection.");
        }
    }

    private static void ValidatePolicy(IdentityProviderConnection connection, ExternalAuthenticationOptions options, IReadOnlySet<string> installedPolicyTypes, ICollection<string> failures)
    {
        var policy = connection.UnlinkedPolicy;
        if (policy is null)
            return;

        if (policy.SettingsVersion <= 0)
            failures.Add($"Configuration connection '{connection.Key}' must use a positive unlinked identity policy settings version.");

        if (!installedPolicyTypes.Contains(policy.Type))
            failures.Add($"Configuration connection '{connection.Key}' selects unlinked identity policy '{policy.Type}', which is not installed.");
        else if (options.AllowedUnlinkedIdentityPolicyTypes.Count > 0 && !options.AllowedUnlinkedIdentityPolicyTypes.Contains(policy.Type, StringComparer.Ordinal))
            failures.Add($"Configuration connection '{connection.Key}' selects unlinked identity policy '{policy.Type}', which is not allowed.");
    }

    private static void ValidateGrantSources(IdentityProviderConnection connection, ExternalAuthenticationOptions options, IReadOnlySet<string> installedGrantSourceTypes, ICollection<string> failures)
    {
        var orders = new HashSet<int>();
        foreach (var grantSource in connection.PermissionGrantSources ?? [])
        {
            if (!orders.Add(grantSource.Order))
                failures.Add($"Configuration connection '{connection.Key}' configures more than one permission grant source at order '{grantSource.Order}'.");

            if (grantSource.SettingsVersion <= 0)
                failures.Add($"Configuration connection '{connection.Key}' must use a positive permission grant source settings version.");

            if (!installedGrantSourceTypes.Contains(grantSource.Type))
                failures.Add($"Configuration connection '{connection.Key}' selects permission grant source '{grantSource.Type}', which is not installed.");
            else if (options.AllowedPermissionGrantSourceTypes.Count > 0 && !options.AllowedPermissionGrantSourceTypes.Contains(grantSource.Type, StringComparer.Ordinal))
                failures.Add($"Configuration connection '{connection.Key}' selects permission grant source '{grantSource.Type}', which is not allowed.");
        }
    }
}
