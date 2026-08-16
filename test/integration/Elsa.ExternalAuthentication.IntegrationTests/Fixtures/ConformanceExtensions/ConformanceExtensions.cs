using System.Text.Json;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.IntegrationTests.Fixtures.ConformanceExtensions;

internal sealed class ConformanceExternalAuthenticationAdapter : IExternalAuthenticationAdapter
{
    public const string AdapterType = "conformance-oauth";
    public string Type => AdapterType;

    public ExternalAuthenticationAdapterDescriptor Describe() => new(
        AdapterType,
        "Conformance OAuth",
        "A deterministic provider-specific OAuth adapter used to verify the protocol-neutral extension boundary.",
        2,
        [
            new SettingFieldDescriptor(
                "issuer",
                "Issuer",
                "Stable external issuer namespace.",
                "uri",
                true,
                "uri",
                null,
                [],
                new SettingFieldValidation(1, 2048),
                false,
                false,
                null,
                null,
                false)
        ],
        new ExternalAuthenticationAdapterCapabilities(true, true, false),
        new CustomEditorContract("conformance-oauth-editor", 1));

    public ValueTask<ConnectionValidationResult> ValidateAsync(ConnectionValidationContext context, CancellationToken cancellationToken = default)
    {
        var valid = context.Connection.Connection.AdapterSettings.TryGetProperty("issuer", out var issuer) &&
            Uri.TryCreate(issuer.GetString(), UriKind.Absolute, out _);
        return ValueTask.FromResult(new ConnectionValidationResult(
            valid,
            valid ? [] : [new ConnectionValidationError("issuer", "required", "A valid issuer is required.")],
            []));
    }

    public ValueTask<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync(ExternalAuthorizationContext context, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"https://provider.example/authorize?state={Uri.EscapeDataString(context.CorrelationState)}");
        return ValueTask.FromResult(new ExternalAuthorizationRequest(uri, []));
    }

    public ValueTask<ExternalAuthenticationResult> AuthenticateCallbackAsync(ExternalCallbackContext context, CancellationToken cancellationToken = default)
    {
        var issuer = context.Connection.Connection.AdapterSettings.GetProperty("issuer").GetString()!;
        var subject = context.Parameters["subject"].Single();
        var claims = new Dictionary<string, IReadOnlyCollection<string>> { ["department"] = ["engineering"] };
        return ValueTask.FromResult(new ExternalAuthenticationResult(new ExternalIdentity(issuer, subject, claims), claims, []));
    }

    public ValueTask<ConnectionTestResult> TestAsync(ConnectionTestContext context, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(new ConnectionTestResult(ConnectionObservationStatus.Succeeded, "conformance", "Conformance adapter is available.", []));

    public ValueTask<ExternalLogoutRequest?> CreateLogoutRequestAsync(ExternalLogoutContext context, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<ExternalLogoutRequest?>(null);
}

internal sealed class ConformanceAdapterSettingsMigration : IAdapterSettingsMigration
{
    public string AdapterType => ConformanceExternalAuthenticationAdapter.AdapterType;
    public int FromVersion => 1;
    public int ToVersion => 2;

    public ValueTask<JsonElement> MigrateAsync(JsonElement settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var authority = settings.GetProperty("authority").GetString();
        return ValueTask.FromResult(JsonSerializer.SerializeToElement(new { issuer = authority }));
    }
}

internal sealed class ConformanceUnlinkedIdentityPolicy : IUnlinkedIdentityPolicy
{
    public const string PolicyType = "conformance-link";
    public string Type => PolicyType;
    public UnlinkedIdentityPolicyDescriptor Describe() => new(
        PolicyType,
        "Conformance link",
        "Links to a configured existing user for conformance testing.",
        1,
        [],
        null);

    public ValueTask<UnlinkedIdentityDecision> EvaluateAsync(UnlinkedIdentityContext context, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<UnlinkedIdentityDecision>(new UnlinkedIdentityDecision.LinkExistingUser("user-a", "conformance"));
}

internal sealed class ConformancePermissionGrantSource : IPermissionGrantSource
{
    public const string SourceType = "conformance-grants";
    public string Type => SourceType;
    public PermissionGrantSourceDescriptor Describe() => new(
        SourceType,
        "Conformance grants",
        "Produces a deterministic permission with provenance for conformance testing.",
        1,
        [],
        null);

    public ValueTask<PermissionGrantResult> GetGrantsAsync(PermissionGrantContext context, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(new PermissionGrantResult(
            [new PermissionGrant("conformance:read", SourceType, "fixture")],
            []));
}
