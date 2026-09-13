using Elsa.Authorization;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Elsa.ExternalAuthentication.IntegrationTests.Fixtures;
using Elsa.Identity.Contracts;
using TUnit.AspNetCore;

namespace Elsa.ExternalAuthentication.IntegrationTests.Operations;

public class PreviewEndpointContractTests : WebApplicationTest<PreviewEndpointContractWebApplicationFactory, ExternalAuthenticationTestEntryPoint>
{
    private const string PreviewHandle = "preview-handle";
    private static readonly Uri ProviderAuthorizationUri = new("https://provider.example/authorize?state=provider-state");
    private HttpClient? _client;
    private readonly TestAuthenticationState _authentication = new();
    private readonly TestClock _clock = new(DateTimeOffset.Parse("2026-07-30T00:00:00Z"));
    private readonly ExternalAuthenticationOptions _options = new();
    private readonly TestHandleHasher _handleHasher = new();
    private readonly TestAdapter _adapter = new();
    private readonly IIdentityProviderConnectionStore _store = Substitute.For<IIdentityProviderConnectionStore>();
    private readonly IConnectionRegistryVersionStore _registryVersions = Substitute.For<IConnectionRegistryVersionStore>();
    private readonly IUnlinkedIdentityPolicyRegistry _policies = Substitute.For<IUnlinkedIdentityPolicyRegistry>();
    private readonly IExternalUserMatcherRegistry _matchers = Substitute.For<IExternalUserMatcherRegistry>();
    private readonly IPermissionGrantSourceRegistry _grantSources = Substitute.For<IPermissionGrantSourceRegistry>();
    private readonly IPermissionDelegationAuthorizer _delegation = Substitute.For<IPermissionDelegationAuthorizer>();
    private readonly IRoleAuthorizationService _roles = Substitute.For<IRoleAuthorizationService>();
    private readonly IExternalAuthenticationSessionStore _sessions = Substitute.For<IExternalAuthenticationSessionStore>();
    private readonly IExternalIdentityProvisioner _provisioner = Substitute.For<IExternalIdentityProvisioner>();
    private readonly IPermissionGrantResolver _permissionGrants = Substitute.For<IPermissionGrantResolver>();
    private readonly ITenantAccessor _tenantAccessor = Substitute.For<ITenantAccessor>();
    private readonly IIdentityProviderConnectionRegistry _connectionRegistry = Substitute.For<IIdentityProviderConnectionRegistry>();
    private readonly IAdapterSettingsMigrationService _settingsMigrations = Substitute.For<IAdapterSettingsMigrationService>();
    private readonly IIdentityProviderConnectionValidityAssessor _validityAssessor = Substitute.For<IIdentityProviderConnectionValidityAssessor>();
    private readonly InMemoryExternalAuthenticationStateStore _stateStore;
    private readonly InMemoryPreviewResultStore _previewResults;

    private HttpClient Client => _client ??= Factory.CreateClient();

    public PreviewEndpointContractTests()
    {
        _authentication.SetClaims(
            new Claim(PermissionNames.ClaimType, $"{ExternalAuthenticationResourcePermissions.Connections}:{ExternalAuthenticationVerbs.Preview}"),
            new Claim(ClaimTypes.NameIdentifier, "administrator-a"));
        _tenantAccessor.TenantId.Returns("tenant-a");
        _stateStore = new InMemoryExternalAuthenticationStateStore(_clock);
        _previewResults = new InMemoryPreviewResultStore(_clock);

        var connection = CreateConnection();
        var effectiveConnection = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.Host, ConnectionValidity.Valid, false, "test");
        _connectionRegistry.FindByIdAsync("tenant-a", connection.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<EffectiveIdentityProviderConnection?>(effectiveConnection));
        _settingsMigrations.MigrateAsync(_adapter.Type, connection.AdapterSettingsVersion, Arg.Any<JsonElement>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new AdapterSettingsMigrationResult(connection.AdapterSettingsVersion, connection.AdapterSettings, false)));
        _validityAssessor.AssessAsync(Arg.Any<EffectiveIdentityProviderConnection>(), Arg.Any<CancellationToken>())
            .Returns(call => ValueTask.FromResult(call.Arg<EffectiveIdentityProviderConnection>()));
    }

    protected override async Task SetupAsync()
    {
        var connection = CreateConnection();
        var expiresAt = _clock.UtcNow.AddMinutes(5);
        await _stateStore.PutAsync("PreviewStart", _handleHasher.Hash(PreviewHandle), new BrokerTransaction
        {
            HandleHash = _handleHasher.Hash(PreviewHandle),
            Purpose = BrokerTransactionPurpose.Preview,
            ClientId = "administrator-a",
            CallbackUri = new Uri($"/external-authentication/previews/{PreviewHandle}/authorize", UriKind.Relative),
            ReturnPath = "/",
            TenantId = "tenant-a",
            ConnectionId = connection.Id,
            ConnectionMaterialRevision = connection.MaterialRevision,
            PkceChallenge = string.Empty,
            ExpiresAt = expiresAt
        }, expiresAt);
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton(_authentication);
        services.AddSingleton(_store);
        services.AddSingleton(_connectionRegistry);
        services.AddSingleton(_validityAssessor);
        services.AddSingleton(_registryVersions);
        services.AddSingleton<IExternalAuthenticationAdapterRegistry>(new TestAdapterRegistry(_adapter));
        services.AddSingleton(_settingsMigrations);
        services.AddSingleton(_policies);
        services.AddSingleton(_matchers);
        services.AddSingleton(_grantSources);
        services.AddSingleton(_delegation);
        services.AddSingleton<IPermissionEvaluator, PermissionEvaluator>();
        services.AddSingleton<ConnectionRevisionCalculator>();
        services.AddSingleton<ISystemClock>(_clock);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(_options));
        services.AddSingleton(_roles);
        services.AddSingleton(_sessions);
        services.AddSingleton(_provisioner);
        services.AddSingleton(_permissionGrants);
        services.AddSingleton<IExternalAuthenticationStateStore>(_stateStore);
        services.AddSingleton<IPreviewResultStore>(_previewResults);
        services.AddSingleton<IExternalAuthenticationHandleHasher>(_handleHasher);
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        services.AddSingleton(_tenantAccessor);
        services.AddScoped<IdentityProviderConnectionManagementService>();
        services.AddScoped<ExternalAuthenticationSecurityNotifier>();
        services.AddScoped<PreviewSignInService>();
    }

    [Test]
    public async Task AuthorizeReturnsProviderRedirectAndConsumedHandleReturnsGone()
    {
        var firstResponse = await Client.GetAsync($"/external-authentication/previews/{PreviewHandle}/authorize");
        var secondResponse = await Client.GetAsync($"/external-authentication/previews/{PreviewHandle}/authorize");

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.Found);
        await Assert.That(firstResponse.Headers.Location).IsEqualTo(ProviderAuthorizationUri);
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.Gone);
    }

    [Test]
    public async Task MissingPreviewResultReturnsNotFound()
    {
        var response = await Client.GetAsync("/external-authentication/previews/missing-handle");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    private static IdentityProviderConnection CreateConnection() => new()
    {
        Id = "connection-a",
        TenantId = ConnectionScope.HostTenantId,
        Key = "connection-a",
        AdapterType = TestAdapter.AdapterType,
        AdapterSettingsVersion = 1,
        AdapterSettings = JsonSerializer.SerializeToElement(new { }),
        DisplayName = "Connection A",
        IsEnabled = true,
        MaterialRevision = "material-revision-a",
        Revision = 1
    };

    private sealed class TestClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class TestHandleHasher : IExternalAuthenticationHandleHasher
    {
        public string Hash(string value) => $"hashed:{value}";
    }

    private sealed class TestAdapterRegistry(IExternalAuthenticationAdapter adapter) : IExternalAuthenticationAdapterRegistry
    {
        public IReadOnlyCollection<ExternalAuthenticationAdapterDescriptor> ListDescriptors() => [adapter.Describe()];

        public bool TryGet(string type, out IExternalAuthenticationAdapter resolved)
        {
            resolved = adapter;
            return string.Equals(type, adapter.Type, StringComparison.Ordinal);
        }
    }

    private sealed class TestAdapter : IExternalAuthenticationAdapter
    {
        public const string AdapterType = "preview-endpoint-test";
        public string Type => AdapterType;

        public ExternalAuthenticationAdapterDescriptor Describe() => new(
            Type,
            "Preview endpoint test",
            "Deterministic adapter for the preview endpoint contract.",
            1,
            [],
            new ExternalAuthenticationAdapterCapabilities(true, true, false),
            null);

        public ValueTask<ConnectionValidationResult> ValidateAsync(ConnectionValidationContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new ConnectionValidationResult(true, [], []));

        public ValueTask<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync(ExternalAuthorizationContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new ExternalAuthorizationRequest(ProviderAuthorizationUri, []));

        public ValueTask<ExternalAuthenticationResult> AuthenticateCallbackAsync(ExternalCallbackContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ConnectionTestResult> TestAsync(ConnectionTestContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalLogoutRequest?> CreateLogoutRequestAsync(ExternalLogoutContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
