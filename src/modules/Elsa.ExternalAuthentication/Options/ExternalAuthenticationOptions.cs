using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Permissions;
using Microsoft.Extensions.Configuration;

namespace Elsa.ExternalAuthentication.Options;

/// <summary>
/// Configures deployment-owned External Authentication behavior.
/// </summary>
public class ExternalAuthenticationOptions
{
    /// <summary>Deployment-owned broker clients. Configuration key: <c>AuthenticationClients</c>.</summary>
    [ConfigurationKeyName("AuthenticationClients")]
    public ICollection<AuthenticationClient> Clients { get; set; } = new List<AuthenticationClient>();

    /// <summary>Controls brokered Elsa username/password login as a normal login method.</summary>
    public LocalLoginMethodOptions LocalLogin { get; set; } = new();

    /// <summary>Immutable configuration-owned connections. Configuration key: <c>Connections</c>.</summary>
    [ConfigurationKeyName("Connections")]
    public ICollection<IdentityProviderConnection> ConfigurationConnections { get; set; } = new List<IdentityProviderConnection>();

    /// <summary>Enables the optional database connection source and persisted management surface.</summary>
    public bool EnableDatabaseConnections { get; set; } = true;

    /// <summary>Adapter types permitted by this deployment. An empty collection permits every installed adapter.</summary>
    public ICollection<string> AllowedAdapterTypes { get; set; } = new List<string>();

    /// <summary>Unlinked identity policy types permitted by this deployment.</summary>
    public ICollection<string> AllowedUnlinkedIdentityPolicyTypes { get; set; } = ["reject", "create-user"];

    /// <summary>Permission grant source types permitted by this deployment.</summary>
    public ICollection<string> AllowedPermissionGrantSourceTypes { get; set; } = ["elsa-roles", "claim-mapping", "group-mapping", "claim-pass-through"];

    /// <summary>Controls the deployment default and database override boundary for unlinked identities.</summary>
    public UnlinkedIdentityPolicyOptions UnlinkedIdentityPolicy { get; set; } = new();

    /// <summary>Defines deployment-wide Elsa permission allow and deny boundaries.</summary>
    public PermissionGrantOptions PermissionGrants { get; set; } = new();

    /// <summary>Configures transaction, code, preview, and session lifetimes.</summary>
    public ExternalAuthenticationLifetimesOptions Lifetimes { get; set; } = new();

    /// <summary>Defines default projected-claim size limits.</summary>
    public ExternalAuthenticationClaimsOptions Claims { get; set; } = new();

    /// <summary>Configures anonymous broker endpoint rate limits.</summary>
    public ExternalAuthenticationRateLimitOptions RateLimits { get; set; } = new();

    /// <summary>Controls provider HTTP destinations, redirects, timeouts, and response limits.</summary>
    public ProviderEgressOptions ProviderEgress { get; set; } = new();

    /// <summary>Controls callback, return-path, and broker-client PKCE validation.</summary>
    public RedirectValidationOptions Redirects { get; set; } = new();

    /// <summary>Reserved broker-side description of WebAssembly persistence; Studio enforces its host-local setting.</summary>
    public WebAssemblyCredentialPersistenceOptions WebAssemblyPersistence { get; set; } = new();

    /// <summary>Defines the deployment default for upstream provider logout.</summary>
    public ExternalAuthenticationLogoutOptions Logout { get; set; } = new();

    /// <summary>Prevents management changes from removing the last normal login path without recovery.</summary>
    public FinalLoginPathGuardOptions FinalLoginPathGuard { get; set; } = new();

    /// <summary>Configures optional operational surfaces.</summary>
    public ExternalAuthenticationOperationsOptions Operations { get; set; } = new();

    /// <summary>Configures stable keyed hashing for persisted opaque handles and external subjects.</summary>
    public ExternalAuthenticationHandleHashingOptions HandleHashing { get; set; } = new();
}

/// <summary>Deployment-owned availability and presentation settings for broker-local credential sign-in.</summary>
public class LocalLoginMethodOptions
{
    /// <summary>Whether brokered local credentials appear as a normal login method.</summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Text shown by login method discovery.</summary>
    public string DisplayName { get; set; } = "Elsa account";
    /// <summary>Trusted server-hosted icon identifier.</summary>
    public string IconId { get; set; } = "elsa";
    /// <summary>Deterministic chooser order.</summary>
    public int DisplayOrder { get; set; }
    /// <summary>Whether local login is the automatic method for its scope.</summary>
    public bool IsDefault { get; set; }
}

/// <summary>Configures the deployment-owned default unlinked identity policy.</summary>
public class UnlinkedIdentityPolicyOptions
{
    /// <summary>Default registered policy type. The safe default rejects unlinked identities.</summary>
    public string DefaultType { get; set; } = "reject";
    /// <summary>Whether a database-owned connection may select another deployment-allowed policy.</summary>
    public bool AllowDatabaseConnectionOverride { get; set; }
}

/// <summary>Defines deployment-level boundaries applied after grant sources resolve candidate permissions.</summary>
public class PermissionGrantOptions
{
    /// <summary>When empty, all syntactically valid permission names are eligible unless denied.</summary>
    public ICollection<string> AllowedPermissions { get; set; } = new List<string>();
    /// <summary>Exact permission names denied regardless of grant source.</summary>
    public ICollection<string> DeniedPermissions { get; set; } = new List<string>();
}

/// <summary>Configures bounded authentication flow and session lifetimes.</summary>
public class ExternalAuthenticationLifetimesOptions
{
    /// <summary>Maximum age of a broker or provider correlation transaction.</summary>
    public TimeSpan BrokerTransactionLifetime { get; set; } = TimeSpan.FromMinutes(10);
    /// <summary>Maximum age of a single-use broker completion code.</summary>
    public TimeSpan CompletionCodeLifetime { get; set; } = TimeSpan.FromMinutes(1);
    /// <summary>Maximum age of preview state and its one-time result.</summary>
    public TimeSpan PreviewLifetime { get; set; } = TimeSpan.FromMinutes(10);
    /// <summary>Absolute maximum external authentication session age.</summary>
    public TimeSpan MaximumSessionAge { get; set; } = TimeSpan.FromHours(8);
}

/// <summary>Defines default bounds for normalized projected external claims.</summary>
public class ExternalAuthenticationClaimsOptions
{
    /// <summary>Maximum number of projected claims.</summary>
    public int MaximumClaimCount { get; set; } = 64;
    /// <summary>Maximum length of one projected string value.</summary>
    public int MaximumValueLength { get; set; } = 1_024;
    /// <summary>Maximum aggregate UTF-8 size of projected claims.</summary>
    public int MaximumTotalBytes { get; set; } = 16 * 1_024;
}

public class ExternalAuthenticationRateLimitOptions
{
    /// <summary>
    /// Selects the partition key used by the broker's named rate-limit policies.
    /// Remote IP is the secure default because it cannot be bypassed by a caller-supplied client identifier.
    /// </summary>
    public ExternalAuthenticationRateLimitPartitionStrategy PartitionStrategy { get; set; } = ExternalAuthenticationRateLimitPartitionStrategy.RemoteIp;
    /// <summary>Login method discovery rate limit.</summary>
    public RateLimitRule Discovery { get; set; } = new(60, TimeSpan.FromMinutes(1));
    /// <summary>External provider initiation rate limit.</summary>
    public RateLimitRule ExternalInitiation { get; set; } = new(20, TimeSpan.FromMinutes(1));
    /// <summary>Broker-local credential initiation rate limit.</summary>
    public RateLimitRule LocalInitiation { get; set; } = new(10, TimeSpan.FromMinutes(1));
    /// <summary>Provider callback rate limit.</summary>
    public RateLimitRule ProviderCallback { get; set; } = new(60, TimeSpan.FromMinutes(1));
    /// <summary>Authorization-code and refresh-token exchange rate limit.</summary>
    public RateLimitRule TokenExchange { get; set; } = new(30, TimeSpan.FromMinutes(1));
}

/// <summary>Selects the server-derived value used to partition anonymous broker rate limits.</summary>
public enum ExternalAuthenticationRateLimitPartitionStrategy
{
    /// <summary>Partition by remote IP address.</summary>
    RemoteIp,
    /// <summary>Partition by registered client identifier and remote IP address.</summary>
    ClientIdAndRemoteIp
}

/// <summary>A fixed-window permit limit.</summary>
public sealed record RateLimitRule(int PermitLimit, TimeSpan Window);

/// <summary>Controls outbound traffic to identity providers.</summary>
public class ProviderEgressOptions
{
    /// <summary>Reject non-HTTPS provider endpoints.</summary>
    public bool RequireHttps { get; set; } = true;
    /// <summary>Allow resolved private, loopback, link-local, or otherwise non-public destinations.</summary>
    public bool AllowPrivateNetworkDestinations { get; set; }
    /// <summary>Maximum redirects followed after validating every destination.</summary>
    public int MaximumRedirects { get; set; } = 3;
    /// <summary>Maximum outbound connection establishment time.</summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
    /// <summary>Maximum complete provider request time.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);
    /// <summary>Maximum discovery document size.</summary>
    public long MaximumDiscoveryResponseBytes { get; set; } = 1 * 1_024 * 1_024;
    /// <summary>Maximum token endpoint response size.</summary>
    public long MaximumTokenResponseBytes { get; set; } = 256 * 1_024;
    /// <summary>Maximum UserInfo response size.</summary>
    public long MaximumUserInfoResponseBytes { get; set; } = 256 * 1_024;
    /// <summary>Optional exact host allowlist. An empty collection does not add a host restriction.</summary>
    public ICollection<string> AllowedHosts { get; set; } = new List<string>();
    /// <summary>Optional deployment-approved HTTP(S) proxy without embedded credentials.</summary>
    public Uri? ProxyUri { get; set; }
}

/// <summary>Controls broker-client callback and return-path validation.</summary>
public class RedirectValidationOptions
{
    /// <summary>Reserved compatibility preference; the broker currently always requires S256 PKCE.</summary>
    public bool RequirePkceS256 { get; set; } = true;
    /// <summary>Allow explicit HTTP loopback callback registrations for development.</summary>
    public bool AllowDevelopmentLoopbackCallbacks { get; set; }
    /// <summary>Reserved configurable limit; the current validator enforces a fixed 2,048-character maximum.</summary>
    public int MaximumReturnPathLength { get; set; } = 2_048;
}

/// <summary>Describes a broker-side WebAssembly persistence preference; the Studio host owns enforcement.</summary>
public class WebAssemblyCredentialPersistenceOptions
{
    /// <summary>Selected credential persistence mode.</summary>
    public BrowserCredentialPersistence Persistence { get; set; } = BrowserCredentialPersistence.Memory;
    /// <summary>Require a deployment-visible warning for persistent browser storage.</summary>
    public bool RequireExplicitPersistentStorageWarning { get; set; } = true;
}

/// <summary>Configures the default upstream sign-out behavior.</summary>
public class ExternalAuthenticationLogoutOptions
{
    /// <summary>Reserved deployment preference; current connections default directly to <see cref="UpstreamLogoutMode.Disabled"/>.</summary>
    public UpstreamLogoutMode DefaultUpstreamLogoutMode { get; set; } = UpstreamLogoutMode.Disabled;
}

/// <summary>Protects deployment access when a connection mutation removes a normal login method.</summary>
public class FinalLoginPathGuardOptions
{
    /// <summary>Enables the guard.</summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Require another normal method, local login, break glass, or privileged confirmation.</summary>
    public bool RequireRecoveryMethod { get; set; } = true;
    /// <summary>Permission required for a confirmed final-login-path override.</summary>
    public string PrivilegedOverridePermission { get; set; } = ExternalAuthenticationPermissions.ProviderTrustUnsafe;
    /// <summary>Set by deployment configuration when a separately hosted break-glass method remains available.</summary>
    public bool HasBreakGlassAuthentication { get; set; }
}

/// <summary>Enables optional operational surfaces without making them readiness dependencies.</summary>
public class ExternalAuthenticationOperationsOptions
{
    /// <summary>Expose permission-guarded external session list and revoke endpoints.</summary>
    public bool EnableSessionAdministration { get; set; } = true;
    /// <summary>Reserved host preference; register the check explicitly with <c>AddExternalAuthenticationHealthCheck</c>.</summary>
    public bool EnableHealthCheck { get; set; }
    /// <summary>Reserved preferred check name for host integration.</summary>
    public string HealthCheckName { get; set; } = "external-authentication";
    /// <summary>Reserved preferred check tags for host integration.</summary>
    public ICollection<string> HealthCheckTags { get; set; } = ["external-authentication", "optional"];
}

/// <summary>
/// Configures the keyed hashes used for opaque broker handles, external subjects, and secret generations.
/// </summary>
public class ExternalAuthenticationHandleHashingOptions
{
    /// <summary>
    /// A base64-encoded key containing at least 256 bits of entropy. All nodes that share External
    /// Authentication persistence must use the same value. When omitted, a process-local key is
    /// generated for single-node development.
    /// </summary>
    public string? SharedKeyBase64 { get; set; }
}
