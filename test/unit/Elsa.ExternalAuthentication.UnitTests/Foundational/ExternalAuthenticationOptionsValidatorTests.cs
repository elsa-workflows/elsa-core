using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Validation;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ExternalAuthenticationOptionsValidatorTests
{
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
    public void RejectsConfigurationConnectionThatCollidesWithInheritedHostKey()
    {
        var options = new ExternalAuthenticationOptions
        {
            ConfigurationConnections =
            [
                RegistryTestData.Connection("host", "*", "contoso"),
                RegistryTestData.Connection("tenant", "tenant-a", "CONTOSO")
            ]
        };

        var result = CreateValidator().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, x => x.Contains("collides with an inherited host-wide connection"));
    }

    [Fact]
    public void RejectsMultipleConfiguredDefaultsWithinTheSameScope()
    {
        var options = new ExternalAuthenticationOptions
        {
            ConfigurationConnections =
            [
                RegistryTestData.Connection("first", "tenant-a", "first", isDefault: true),
                RegistryTestData.Connection("second", "tenant-a", "second", isDefault: true)
            ]
        };

        var result = CreateValidator().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, x => x.Contains("more than one automatic default"));
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
