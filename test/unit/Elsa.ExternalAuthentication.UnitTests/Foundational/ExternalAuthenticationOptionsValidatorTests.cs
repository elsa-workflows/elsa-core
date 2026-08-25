using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Validation;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ExternalAuthenticationOptionsValidatorTests
{
    [Theory]
    // The legacy spelling carries two colons, so it parses as nothing and would silently stop bounding anything.
    [InlineData("external-authentication:connections:read")]
    [InlineData("not a permission")]
    [InlineData("workflows/definitions:")]
    public void RejectsAGrantBoundaryEntryThatIsNotAWellFormedPermission(string permission)
    {
        var allowed = new ExternalAuthenticationOptions();
        allowed.PermissionGrants.AllowedPermissions = [permission];
        var denied = new ExternalAuthenticationOptions();
        denied.PermissionGrants.DeniedPermissions = [permission];

        var allowedResult = CreateValidator().Validate(null, allowed);
        var deniedResult = CreateValidator().Validate(null, denied);

        Assert.False(allowedResult.Succeeded);
        Assert.Contains(allowedResult.Failures!, x => x.Contains("AllowedPermissions") && x.Contains("well-formed permission"));
        Assert.False(deniedResult.Succeeded);
        Assert.Contains(deniedResult.Failures!, x => x.Contains("DeniedPermissions") && x.Contains("well-formed permission"));
    }

    [Fact]
    public void AcceptsAGrantBoundaryOfWildcardPatterns()
    {
        var options = new ExternalAuthenticationOptions();
        options.PermissionGrants.AllowedPermissions = ["workflows/*:delete", "*"];
        options.PermissionGrants.DeniedPermissions = ["workflows/definitions:*"];

        var result = CreateValidator().Validate(null, options);

        Assert.DoesNotContain(result.Failures ?? [], x => x.Contains("well-formed permission"));
    }

    [Fact]
    public void RejectsDuplicateInstalledAdapterTypes()
    {
        var result = CreateValidator([new StubAdapter("oidc"), new StubAdapter("oidc")]).Validate(null, new ExternalAuthenticationOptions());

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, x => x.Contains("registered more than once"));
    }

    [Fact]
    public void RejectsPublicClientWithWildcardOriginAndSecret()
    {
        var options = new ExternalAuthenticationOptions
        {
            Clients =
            [
                new AuthenticationClient(
                    "studio",
                    "Studio",
                    AuthenticationClientType.Public,
                    new HashSet<Uri> { new("https://studio.example/callback") },
                    new HashSet<Uri>(),
                    new HashSet<string> { "https://*.example" },
                    new HashSet<string> { "/" },
                    new SecretBinding("configuration", "studio-secret"),
                    true)
            ]
        };

        var result = CreateValidator().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, x => x.Contains("invalid allowed origin"));
        Assert.Contains(result.Failures!, x => x.Contains("must not define a client secret"));
    }

    [Fact]
    public void RejectsNonHostConfigurationConnection()
    {
        var options = new ExternalAuthenticationOptions
        {
            ConfigurationConnections =
            [
                RegistryTestData.Connection("tenant", "tenant-a", "contoso")
            ]
        };

        var result = CreateValidator().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, x => x.Contains("must use the host scope"));
    }

    [Fact]
    public void RejectsMultipleConfiguredPreferredConnections()
    {
        var options = new ExternalAuthenticationOptions
        {
            ConfigurationConnections =
            [
                RegistryTestData.Connection("first", "*", "first", isPreferred: true),
                RegistryTestData.Connection("second", "*", "second", isPreferred: true)
            ]
        };

        var result = CreateValidator().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, x => x.Contains("more than one preferred sign-in method"));
    }

    [Fact]
    public void RejectsNonPositiveRateLimitRules()
    {
        var options = new ExternalAuthenticationOptions
        {
            RateLimits = new ExternalAuthenticationRateLimitOptions
            {
                Discovery = new RateLimitRule(0, TimeSpan.Zero)
            }
        };

        var result = CreateValidator().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, x => x.Contains("Discovery") && x.Contains("positive permit limit and window"));
    }

    [Theory]
    [InlineData("http://elsa.example")]
    [InlineData("https://elsa.example/?unexpected=true")]
    public void RejectsUnsafeExternalCallbackBaseUri(string callbackBaseUri)
    {
        var options = new ExternalAuthenticationOptions
        {
            Redirects = new RedirectValidationOptions { ExternalCallbackBaseUri = new Uri(callbackBaseUri) }
        };

        var result = CreateValidator().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("ExternalCallbackBaseUri"));
    }

    [Fact]
    public void AllowsHttpLoopbackExternalCallbackBaseUriOnlyWhenDevelopmentModeIsEnabled()
    {
        var options = new ExternalAuthenticationOptions
        {
            Redirects = new RedirectValidationOptions
            {
                ExternalCallbackBaseUri = new Uri("http://127.0.0.1:5000"),
                AllowDevelopmentLoopbackCallbacks = true
            }
        };

        var result = CreateValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void AcceptsExactPublicClientAndInstalledConfigurationSelections()
    {
        var options = new ExternalAuthenticationOptions
        {
            AllowedAdapterTypes = ["oidc"],
            Clients =
            [
                new AuthenticationClient(
                    "studio",
                    "Studio",
                    AuthenticationClientType.Public,
                    new HashSet<Uri> { new("https://studio.example/callback") },
                    new HashSet<Uri> { new("https://studio.example/logout") },
                    new HashSet<string> { "https://studio.example" },
                    new HashSet<string> { "/" },
                    null,
                    true)
            ],
            ConfigurationConnections = [RegistryTestData.Connection("connection", "*", "contoso")]
        };

        var result = CreateValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    private static ExternalAuthenticationOptionsValidator CreateValidator(IEnumerable<StubAdapter>? adapters = null)
    {
        var extensions = new ExternalAuthenticationExtensionOptions();
        foreach (var adapter in adapters ?? [new StubAdapter("oidc")])
            extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.Adapter, adapter.Type));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.UnlinkedIdentityPolicy, "reject"));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.UnlinkedIdentityPolicy, "create-user"));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.PermissionGrantSource, "elsa-roles"));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.PermissionGrantSource, "claim-mapping"));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.PermissionGrantSource, "group-mapping"));
        extensions.Registrations.Add(new(ExternalAuthenticationExtensionKind.PermissionGrantSource, "claim-pass-through"));
        return new(Microsoft.Extensions.Options.Options.Create(extensions));
    }
}
