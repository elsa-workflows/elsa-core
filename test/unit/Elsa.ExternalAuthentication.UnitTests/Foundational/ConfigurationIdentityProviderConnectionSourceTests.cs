using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Providers;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Contracts;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ConfigurationIdentityProviderConnectionSourceTests
{
    [Fact]
    public async Task MaterializesOnlyRequestedScopeWithStableGeneratedIdAndSnapshotVersion()
    {
        var options = new ExternalAuthenticationOptions
        {
            ConfigurationConnections =
            [
                RegistryTestData.Connection(string.Empty, "*", " Contoso ", displayName: "Contoso"),
                RegistryTestData.Connection("tenant", "tenant-a", "fabrikam")
            ]
        };
        var monitor = new MutableOptionsMonitor<ExternalAuthenticationOptions>(options);
        var calculator = new ConnectionRevisionCalculator();
        var source = new ConfigurationIdentityProviderConnectionSource(monitor, calculator, EmptyAdapterRegistry.Instance);

        var first = await source.GetSnapshotAsync(ConnectionScope.Host);
        var second = await source.GetSnapshotAsync(ConnectionScope.Host);

        var connection = Assert.Single(first.Connections);
        Assert.Equal("contoso", connection.Key);
        Assert.Equal(ConnectionRevisionCalculator.CalculateConfigurationConnectionId(ConnectionScope.Host, "contoso"), connection.Id);
        Assert.Equal(first.Version, second.Version);
        Assert.StartsWith("m-", connection.MaterialRevision);
    }

    [Fact]
    public async Task DoesNotReturnMutableConfigurationObjects()
    {
        var configuredConnection = RegistryTestData.Connection("connection");
        var source = new ConfigurationIdentityProviderConnectionSource(
            new MutableOptionsMonitor<ExternalAuthenticationOptions>(new ExternalAuthenticationOptions { ConfigurationConnections = [configuredConnection] }),
            new ConnectionRevisionCalculator(),
            EmptyAdapterRegistry.Instance);

        var snapshot = await source.GetSnapshotAsync(ConnectionScope.Host);
        var materializedConnection = Assert.Single(snapshot.Connections);
        materializedConnection.DisplayName = "Changed";

        Assert.Equal("Contoso", configuredConnection.DisplayName);
    }

    [Fact]
    public async Task RejectsDescriptorDeclaredSecretValuesInConfigurationSettings()
    {
        var configuredConnection = RegistryTestData.Connection("connection");
        configuredConnection.AdapterType = "test";
        configuredConnection.AdapterSettings = System.Text.Json.JsonDocument.Parse("{\"clientSecret\":\"not-a-binding\"}").RootElement.Clone();
        var source = new ConfigurationIdentityProviderConnectionSource(
            new MutableOptionsMonitor<ExternalAuthenticationOptions>(new ExternalAuthenticationOptions { ConfigurationConnections = [configuredConnection] }),
            new ConnectionRevisionCalculator(),
            new SecretDeclaringAdapterRegistry());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => source.GetSnapshotAsync(ConnectionScope.Host).AsTask());

        Assert.Contains("SecretBindings", exception.Message, StringComparison.Ordinal);
    }

    private sealed class EmptyAdapterRegistry : IExternalAuthenticationAdapterRegistry
    {
        public static readonly EmptyAdapterRegistry Instance = new();
        public IReadOnlyCollection<ExternalAuthenticationAdapterDescriptor> ListDescriptors() => [];
        public bool TryGet(string type, out IExternalAuthenticationAdapter adapter)
        {
            adapter = null!;
            return false;
        }
    }

    private sealed class SecretDeclaringAdapterRegistry : IExternalAuthenticationAdapterRegistry
    {
        private readonly IExternalAuthenticationAdapter _adapter = new SecretDeclaringAdapter();
        public IReadOnlyCollection<ExternalAuthenticationAdapterDescriptor> ListDescriptors() => [_adapter.Describe()];
        public bool TryGet(string type, out IExternalAuthenticationAdapter adapter)
        {
            adapter = _adapter;
            return string.Equals(type, adapter.Type, StringComparison.Ordinal);
        }
    }

    private sealed class SecretDeclaringAdapter : IExternalAuthenticationAdapter
    {
        public string Type => "test";
        public ExternalAuthenticationAdapterDescriptor Describe() => new(Type, "Test", "Test", 1,
            [new SettingFieldDescriptor("clientSecret", "Client secret", "Secret", "secret", false, "secret", null, [], new SettingFieldValidation(), true, false, null, null, true)],
            new ExternalAuthenticationAdapterCapabilities(false, false, false),
            null);
        public ValueTask<ConnectionValidationResult> ValidateAsync(ConnectionValidationContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync(ExternalAuthorizationContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalAuthenticationResult> AuthenticateCallbackAsync(ExternalCallbackContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ConnectionTestResult> TestAsync(ConnectionTestContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalLogoutRequest?> CreateLogoutRequestAsync(ExternalLogoutContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
