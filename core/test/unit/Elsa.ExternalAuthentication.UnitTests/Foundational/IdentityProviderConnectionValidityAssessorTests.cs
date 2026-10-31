using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class IdentityProviderConnectionValidityAssessorTests
{
    [Fact]
    public async Task MissingRequiredSecretMakesAnEnabledConnectionInvalid()
    {
        var connection = ExternalAuthenticationTestData.CreateConnection();
        connection.AdapterType = RequiredSecretAdapter.AdapterType;
        connection.SecretBindings.Clear();
        var effective = new EffectiveIdentityProviderConnection(
            connection,
            ConnectionSourceOwnership.Database,
            ConnectionScope.Host,
            ConnectionValidity.Unknown,
            false,
            "database");
        var assessor = new IdentityProviderConnectionValidityAssessor(
            new TestAdapterRegistry(new RequiredSecretAdapter()),
            new PassThroughSettingsMigrationService(),
            [],
            new FixedClock());

        var result = await assessor.AssessAsync(effective);

        Assert.Equal(ConnectionValidity.Invalid, result.Validity);
    }

    [Fact]
    public async Task SecretResolverFailureMakesOnlyThatConnectionInvalid()
    {
        var connection = ExternalAuthenticationTestData.CreateConnection();
        connection.AdapterType = RequiredSecretAdapter.AdapterType;
        connection.SecretBindings["clientSecret"] = new SecretBinding(ThrowingSecretBindingResolver.ResolverType, "client-secret");
        var effective = new EffectiveIdentityProviderConnection(
            connection,
            ConnectionSourceOwnership.Database,
            ConnectionScope.Host,
            ConnectionValidity.Unknown,
            false,
            "database");
        var assessor = new IdentityProviderConnectionValidityAssessor(
            new TestAdapterRegistry(new RequiredSecretAdapter()),
            new PassThroughSettingsMigrationService(),
            [new ThrowingSecretBindingResolver()],
            new FixedClock());

        var result = await assessor.AssessAsync(effective);

        Assert.Equal(ConnectionValidity.Invalid, result.Validity);
    }

    private sealed class TestAdapterRegistry(IExternalAuthenticationAdapter adapter) : IExternalAuthenticationAdapterRegistry
    {
        public IReadOnlyCollection<ExternalAuthenticationAdapterDescriptor> ListDescriptors() => [adapter.Describe()];

        public bool TryGet(string type, out IExternalAuthenticationAdapter result)
        {
            result = adapter;
            return string.Equals(type, adapter.Type, StringComparison.Ordinal);
        }
    }

    private sealed class PassThroughSettingsMigrationService : IAdapterSettingsMigrationService
    {
        public ValueTask<AdapterSettingsMigrationResult> MigrateAsync(
            string adapterType,
            int settingsVersion,
            System.Text.Json.JsonElement settings,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AdapterSettingsMigrationResult(settingsVersion, settings, false));
    }

    private sealed class RequiredSecretAdapter : IExternalAuthenticationAdapter
    {
        public const string AdapterType = "required-secret";
        public string Type => AdapterType;

        public ExternalAuthenticationAdapterDescriptor Describe() => new(
            Type,
            "Required secret",
            "Requires a client secret",
            1,
            [new SettingFieldDescriptor("clientSecret", "Client secret", "Secret", "secret", true, "secret", null, [], new SettingFieldValidation(), true, false, null, null, true)],
            new ExternalAuthenticationAdapterCapabilities(false, false, false),
            null);

        public ValueTask<ConnectionValidationResult> ValidateAsync(ConnectionValidationContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new ConnectionValidationResult(true, [], []));
        public ValueTask<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync(ExternalAuthorizationContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalAuthenticationResult> AuthenticateCallbackAsync(ExternalCallbackContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ConnectionTestResult> TestAsync(ConnectionTestContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalLogoutRequest?> CreateLogoutRequestAsync(ExternalLogoutContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ThrowingSecretBindingResolver : ISecretBindingResolver
    {
        public const string ResolverType = "throwing";
        public string Type => ResolverType;
        public ValueTask<SecretBindingState> GetStateAsync(SecretBinding binding, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Secret backend unavailable.");
        public ValueTask<ResolvedSecretBinding> ResolveAsync(SecretBinding binding, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => new(2026, 8, 2, 0, 0, 0, TimeSpan.Zero);
    }
}
