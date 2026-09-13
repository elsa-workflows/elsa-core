using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Validation;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ExternalAuthenticationOptionsValidatorTests
{
    private readonly CapturingLogger<ExternalAuthenticationOptionsValidator> _logger = new();

    [Test]
    // The legacy spelling carries two colons, so it parses as nothing and would silently stop bounding anything.
    [Arguments("external-authentication:connections:read")]
    [Arguments("not a permission")]
    [Arguments("workflows/definitions:")]
    public async Task RejectsAGrantBoundaryEntryThatIsNotAWellFormedPermission(string permission)
    {
        var allowed = new ExternalAuthenticationOptions();
        allowed.PermissionGrants.AllowedPermissions = [permission];
        var denied = new ExternalAuthenticationOptions();
        denied.PermissionGrants.DeniedPermissions = [permission];

        var allowedResult = CreateValidator().Validate(null, allowed);
        var deniedResult = CreateValidator().Validate(null, denied);

        await Assert.That(allowedResult.Succeeded).IsFalse();
        await Assert.That(allowedResult.Failures!).Contains(x => x.Contains("AllowedPermissions") && x.Contains("well-formed permission"));
        await Assert.That(deniedResult.Succeeded).IsFalse();
        await Assert.That(deniedResult.Failures!).Contains(x => x.Contains("DeniedPermissions") && x.Contains("well-formed permission"));
    }

    [Test]
    // These parse, so they slipped past well-formedness — yet the matcher never satisfies them. In a deny
    // list that is a silent un-denying, exactly what boundary validation exists to prevent.
    [Arguments("workflows*:delete")]
    [Arguments("work*/foo:view")]
    [Arguments("work*/definitions/*:view")]
    [Arguments("workflows:del*")]
    public async Task RejectsAGrantBoundaryEntryWithAWildcardTheMatcherNeverSatisfies(string permission)
    {
        var allowed = new ExternalAuthenticationOptions();
        allowed.PermissionGrants.AllowedPermissions = [permission];
        var denied = new ExternalAuthenticationOptions();
        denied.PermissionGrants.DeniedPermissions = [permission];

        var allowedResult = CreateValidator().Validate(null, allowed);
        var deniedResult = CreateValidator().Validate(null, denied);

        await Assert.That(allowedResult.Succeeded).IsFalse();
        await Assert.That(allowedResult.Failures!).Contains(x => x.Contains("AllowedPermissions") && x.Contains("would match nothing"));
        await Assert.That(deniedResult.Succeeded).IsFalse();
        await Assert.That(deniedResult.Failures!).Contains(x => x.Contains("DeniedPermissions") && x.Contains("would match nothing"));
    }

    [Test]
    public async Task AcceptsAGrantBoundaryOfWildcardPatterns()
    {
        var options = new ExternalAuthenticationOptions();
        options.PermissionGrants.AllowedPermissions = ["workflows/*:delete", "*"];
        options.PermissionGrants.DeniedPermissions = ["workflows/definitions:*"];

        var result = CreateValidator().Validate(null, options);

        await Assert.That(result.Failures ?? []).DoesNotContain(x => x.Contains("well-formed permission"));
    }

    /// <remarks>
    /// A deny list is not a failure, but it costs a wildcard grant everything it could have conferred: deny
    /// matches in both directions, so '*' satisfies every deny entry and an externally-authenticated superuser
    /// silently loses it while local login keeps working. The warning names that at startup rather than leaving
    /// it to be diagnosed from an issued token.
    /// </remarks>
    [Test]
    public async Task WarnsThatANonEmptyDenyListRefusesWildcardGrantsWhole()
    {
        var options = new ExternalAuthenticationOptions();
        options.PermissionGrants.DeniedPermissions = ["workflows/*:delete"];

        var result = CreateValidator().Validate(null, options);

        await Assert.That(result.Succeeded).IsTrue();
        var warning = await Assert.That(_logger.Entries).HasSingleItem(x => x.Level == LogLevel.Warning);
        await Assert.That(warning.Message).Contains("DeniedPermissions");
        await Assert.That(warning.Message).Contains("refused entirely");
    }

    [Test]
    public async Task DoesNotWarnWhenNoPermissionsAreDenied()
    {
        var options = new ExternalAuthenticationOptions();
        options.PermissionGrants.AllowedPermissions = ["workflows/*:delete"];

        CreateValidator().Validate(null, options);

        await Assert.That(_logger.Entries).DoesNotContain(x => x.Level == LogLevel.Warning);
    }

    [Test]
    public async Task RejectsDuplicateInstalledAdapterTypes()
    {
        var result = CreateValidator([new StubAdapter("oidc"), new StubAdapter("oidc")]).Validate(null, new ExternalAuthenticationOptions());

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Failures!).Contains(x => x.Contains("registered more than once"));
    }

    [Test]
    public async Task RejectsPublicClientWithWildcardOriginAndSecret()
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

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Failures!).Contains(x => x.Contains("invalid allowed origin"));
        await Assert.That(result.Failures!).Contains(x => x.Contains("must not define a client secret"));
    }

    [Test]
    public async Task RejectsNonHostConfigurationConnection()
    {
        var options = new ExternalAuthenticationOptions
        {
            ConfigurationConnections =
            [
                RegistryTestData.Connection("tenant", "tenant-a", "contoso")
            ]
        };

        var result = CreateValidator().Validate(null, options);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Failures!).Contains(x => x.Contains("must use the host scope"));
    }

    [Test]
    public async Task RejectsMultipleConfiguredPreferredConnections()
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

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Failures!).Contains(x => x.Contains("more than one preferred sign-in method"));
    }

    [Test]
    public async Task RejectsNonPositiveRateLimitRules()
    {
        var options = new ExternalAuthenticationOptions
        {
            RateLimits = new ExternalAuthenticationRateLimitOptions
            {
                Discovery = new RateLimitRule(0, TimeSpan.Zero)
            }
        };

        var result = CreateValidator().Validate(null, options);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Failures!).Contains(x => x.Contains("Discovery") && x.Contains("positive permit limit and window"));
    }

    [Test]
    [Arguments("http://elsa.example")]
    [Arguments("https://elsa.example/?unexpected=true")]
    public async Task RejectsUnsafeExternalCallbackBaseUri(string callbackBaseUri)
    {
        var options = new ExternalAuthenticationOptions
        {
            Redirects = new RedirectValidationOptions { ExternalCallbackBaseUri = new Uri(callbackBaseUri) }
        };

        var result = CreateValidator().Validate(null, options);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Failures!).Contains(failure => failure.Contains("ExternalCallbackBaseUri"));
    }

    [Test]
    public async Task AllowsHttpLoopbackExternalCallbackBaseUriOnlyWhenDevelopmentModeIsEnabled()
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

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task AcceptsExactPublicClientAndInstalledConfigurationSelections()
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

        await Assert.That(result.Succeeded).IsTrue();
    }

    private ExternalAuthenticationOptionsValidator CreateValidator(IEnumerable<StubAdapter>? adapters = null)
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
        return new(Microsoft.Extensions.Options.Options.Create(extensions), _logger);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add(new(logLevel, formatter(state, exception)));
    }

    private sealed record LogEntry(LogLevel Level, string Message);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
