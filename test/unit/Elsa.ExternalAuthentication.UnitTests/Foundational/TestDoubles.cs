using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

internal sealed class MutableOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T Value { get; set; } = value;
    public T CurrentValue => Value;
    public T Get(string? name) => Value;
    public IDisposable OnChange(Action<T, string?> listener) => NoopDisposable.Instance;

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();
        public void Dispose() { }
    }
}

internal sealed class SnapshotSource(
    string name,
    ConnectionSourceOwnership ownership,
    Func<ConnectionScope, ConnectionSourceSnapshot> getSnapshot) : IIdentityProviderConnectionSource
{
    public string Name => name;
    public ConnectionSourceOwnership Ownership => ownership;
    public ValueTask<ConnectionSourceSnapshot> GetSnapshotAsync(ConnectionScope scope, CancellationToken cancellationToken = default) => ValueTask.FromResult(getSnapshot(scope));
}

internal sealed class StubAdapter(string type) : IExternalAuthenticationAdapter
{
    public string Type => type;
    public ExternalAuthenticationAdapterDescriptor Describe() => throw new NotSupportedException();
    public ValueTask<ConnectionValidationResult> ValidateAsync(ConnectionValidationContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public ValueTask<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync(ExternalAuthorizationContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public ValueTask<ExternalAuthenticationResult> AuthenticateCallbackAsync(ExternalCallbackContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public ValueTask<ConnectionTestResult> TestAsync(ConnectionTestContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public ValueTask<ExternalLogoutRequest?> CreateLogoutRequestAsync(ExternalLogoutContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

internal sealed class StubPolicy(string type) : IUnlinkedIdentityPolicy
{
    public string Type => type;
    public UnlinkedIdentityPolicyDescriptor Describe() => throw new NotSupportedException();
    public ValueTask<UnlinkedIdentityDecision> EvaluateAsync(UnlinkedIdentityContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

internal sealed class StubGrantSource(string type) : IPermissionGrantSource
{
    public string Type => type;
    public PermissionGrantSourceDescriptor Describe() => throw new NotSupportedException();
    public ValueTask<PermissionGrantResult> GetGrantsAsync(PermissionGrantContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

internal static class RegistryTestData
{
    public static IdentityProviderConnection Connection(
        string id,
        string tenantId = "*",
        string key = "contoso",
        string displayName = "Contoso",
        int displayOrder = 0,
        bool enabled = true,
        bool isDefault = false) => new()
    {
        Id = id,
        TenantId = tenantId,
        Key = key,
        AdapterType = "oidc",
        AdapterSettingsVersion = 1,
        DisplayName = displayName,
        DisplayOrder = displayOrder,
        IsEnabled = enabled,
        IsDefault = isDefault,
        MaterialRevision = "test"
    };

    public static ConnectionSourceSnapshot Snapshot(ConnectionScope scope, string version, params IdentityProviderConnection[] connections) => new(scope, version, connections);
}
