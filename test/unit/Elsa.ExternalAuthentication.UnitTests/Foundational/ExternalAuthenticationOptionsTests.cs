using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Validation;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ExternalAuthenticationOptionsTests
{
    [Fact]
    public void DefaultsFavorTheMostRestrictiveOperationalSettings()
    {
        var options = new ExternalAuthenticationOptions();

        Assert.True(options.EnableDatabaseConnections);
        Assert.Equal(TimeSpan.FromMinutes(10), options.Lifetimes.BrokerTransactionLifetime);
        Assert.Equal(TimeSpan.FromMinutes(1), options.Lifetimes.CompletionCodeLifetime);
        Assert.Equal(64, options.Claims.MaximumClaimCount);
        Assert.Equal(1_024, options.Claims.MaximumValueLength);
        Assert.Equal(16 * 1_024, options.Claims.MaximumTotalBytes);
        Assert.True(options.ProviderEgress.RequireHttps);
        Assert.False(options.ProviderEgress.AllowPrivateNetworkDestinations);
        Assert.Equal(3, options.ProviderEgress.MaximumRedirects);
        Assert.True(options.Redirects.RequirePkceS256);
        Assert.False(options.Redirects.AllowDevelopmentLoopbackCallbacks);
        Assert.Equal(BrowserCredentialPersistence.Memory, options.WebAssemblyPersistence.Persistence);
        Assert.True(options.WebAssemblyPersistence.RequireExplicitPersistentStorageWarning);
        Assert.True(options.FinalLoginPathGuard.IsEnabled);
        Assert.True(options.FinalLoginPathGuard.RequireRecoveryMethod);
    }

    [Fact]
    public void ValidatorRejectsDuplicateOrUnavailableExtensionTypes()
    {
        var options = new ExternalAuthenticationOptions
        {
            AllowedAdapterTypes = ["oidc", "oidc", "saml"],
            AllowedUnlinkedIdentityPolicyTypes = ["reject"],
            AllowedPermissionGrantSourceTypes = ["elsa-roles"]
        };
        var validator = CreateValidator(adapters: [new TestAdapter("oidc"), new TestAdapter("oidc")]);

        var result = validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("adapter type 'oidc' is registered more than once", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains("allowed adapter type 'oidc' is configured more than once", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains("allowed adapter type 'saml' is not installed", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidatorRejectsInsecureClientRegistrations()
    {
        var options = CreateValidOptions();
        options.Clients =
        [
            new AuthenticationClient(
                "studio",
                "Studio",
                AuthenticationClientType.Public,
                new HashSet<Uri> { new("http://studio.example/callback") },
                new HashSet<Uri>(),
                new HashSet<string> { "https://studio.example/" },
                new HashSet<string>(),
                new SecretBinding("test", "must-not-be-set"),
                true)
        ];

        var result = CreateValidator().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("invalid callback URI", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains("must not define a client secret binding", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains("must not contain a path, query, or trailing slash", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidatorRejectsUnsafeProviderEgressConfiguration()
    {
        var options = CreateValidOptions();
        options.ProviderEgress = new ProviderEgressOptions
        {
            MaximumRedirects = -1,
            ConnectTimeout = TimeSpan.Zero,
            RequestTimeout = TimeSpan.Zero,
            MaximumDiscoveryResponseBytes = 0,
            MaximumTokenResponseBytes = 0,
            MaximumUserInfoResponseBytes = 0,
            AllowedHosts = ["*.example.test"],
            ProxyUri = new Uri("ftp://user:password@proxy.example")
        };

        var result = CreateValidator().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("egress maximum redirects", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains("egress timeouts", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains("response-size limits", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains("allowed host", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains("proxy URI", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidatorRejectsConfigurationCollisionsAndUnknownAdapterTypes()
    {
        var options = CreateValidOptions();
        var unknownAdapter = ExternalAuthenticationTestData.CreateConnection("unknown", "tenant-b", "partner");
        unknownAdapter.AdapterType = "saml";
        options.ConfigurationConnections =
        [
            ExternalAuthenticationTestData.CreateConnection("host", ConnectionScope.HostTenantId, "corp"),
            ExternalAuthenticationTestData.CreateConnection("tenant", "tenant-a", " CORP "),
            unknownAdapter
        ];

        var result = CreateValidator().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("collides with an inherited host-wide connection", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains("selects adapter type 'saml', which is not installed", StringComparison.Ordinal));
    }

    private static ExternalAuthenticationOptions CreateValidOptions() => new()
    {
        AllowedAdapterTypes = ["oidc"],
        AllowedUnlinkedIdentityPolicyTypes = ["reject"],
        AllowedPermissionGrantSourceTypes = ["elsa-roles"]
    };

    private static ExternalAuthenticationOptionsValidator CreateValidator(
        IEnumerable<IExternalAuthenticationAdapter>? adapters = null)
    {
        var extensions = new ExternalAuthenticationExtensionOptions();
        foreach (var adapter in adapters ?? [new TestAdapter("oidc")])
            extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.Adapter, adapter.Type));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.UnlinkedIdentityPolicy, "reject"));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.PermissionGrantSource, "elsa-roles"));
        return new(Microsoft.Extensions.Options.Options.Create(extensions));
    }

    private sealed class TestAdapter(string type) : IExternalAuthenticationAdapter
    {
        public string Type => type;
        public ExternalAuthenticationAdapterDescriptor Describe() => throw new NotSupportedException();
        public ValueTask<ConnectionValidationResult> ValidateAsync(ConnectionValidationContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync(ExternalAuthorizationContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalAuthenticationResult> AuthenticateCallbackAsync(ExternalCallbackContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ConnectionTestResult> TestAsync(ConnectionTestContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalLogoutRequest?> CreateLogoutRequestAsync(ExternalLogoutContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestUnlinkedIdentityPolicy(string type) : IUnlinkedIdentityPolicy
    {
        public string Type => type;
        public UnlinkedIdentityPolicyDescriptor Describe() => throw new NotSupportedException();
        public ValueTask<UnlinkedIdentityDecision> EvaluateAsync(UnlinkedIdentityContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestPermissionGrantSource(string type) : IPermissionGrantSource
    {
        public string Type => type;
        public PermissionGrantSourceDescriptor Describe() => throw new NotSupportedException();
        public ValueTask<PermissionGrantResult> GetGrantsAsync(PermissionGrantContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
