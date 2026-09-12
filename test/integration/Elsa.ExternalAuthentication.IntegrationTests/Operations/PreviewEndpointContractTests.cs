using Elsa.Authorization;
using System.Net;
using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Features;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Elsa.ExternalAuthentication.IntegrationTests.Fixtures;

namespace Elsa.ExternalAuthentication.IntegrationTests.Operations;

[Collection(nameof(EndpointSecurityCollection))]
public class PreviewEndpointContractTests : IAsyncLifetime
{
    private const string PreviewHandle = "preview-handle";
    private static readonly Uri ProviderAuthorizationUri = new("https://provider.example/authorize?state=provider-state");
    private WebApplication? _app;
    private HttpClient? _client;
    private bool _wasSecurityEnabled;

    public async Task InitializeAsync()
    {
        _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = false;

        var clock = new TestClock(DateTimeOffset.Parse("2026-07-30T00:00:00Z"));
        var options = Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions());
        var stateStore = new InMemoryExternalAuthenticationStateStore(clock);
        var handleHasher = new TestHandleHasher();
        var adapter = new TestAdapter();
        var adapters = new TestAdapterRegistry(adapter);
        var connection = CreateConnection();
        var effectiveConnection = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.Host, ConnectionValidity.Valid, false, "test");
        var connectionRegistry = Substitute.For<IIdentityProviderConnectionRegistry>();
        connectionRegistry.FindByIdAsync("tenant-a", connection.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<EffectiveIdentityProviderConnection?>(effectiveConnection));
        var settingsMigrations = Substitute.For<IAdapterSettingsMigrationService>();
        settingsMigrations.MigrateAsync(adapter.Type, connection.AdapterSettingsVersion, Arg.Any<JsonElement>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new AdapterSettingsMigrationResult(connection.AdapterSettingsVersion, connection.AdapterSettings, false)));
        var validityAssessor = Substitute.For<IIdentityProviderConnectionValidityAssessor>();
        validityAssessor.AssessAsync(Arg.Any<EffectiveIdentityProviderConnection>(), Arg.Any<CancellationToken>())
            .Returns(call => ValueTask.FromResult(call.Arg<EffectiveIdentityProviderConnection>()));
        var management = new IdentityProviderConnectionManagementService(
            null!,
            connectionRegistry,
            validityAssessor,
            null!,
            adapters,
            settingsMigrations,
            null!,
            null!,
            null!,
            [],
            [],
            null!,
            new PermissionEvaluator(),
            null!,
            clock,
            options,
            null!,
            null!,
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<IdentityProviderConnectionManagementService>.Instance);
        var previews = new PreviewSignInService(
            management,
            adapters,
            [],
            [],
            Substitute.For<IExternalIdentityProvisioner>(),
            Substitute.For<IPermissionGrantResolver>(),
            stateStore,
            new InMemoryPreviewResultStore(clock),
            handleHasher,
            new EphemeralDataProtectionProvider(),
            clock,
            options,
            new ExternalAuthenticationSecurityNotifier(new ServiceCollection().BuildServiceProvider()));
        var expiresAt = clock.UtcNow.AddMinutes(5);
        await stateStore.PutAsync("PreviewStart", handleHasher.Hash(PreviewHandle), new BrokerTransaction
        {
            HandleHash = handleHasher.Hash(PreviewHandle),
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

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddFastEndpoints(endpointOptions =>
        {
            endpointOptions.Assemblies = [typeof(ExternalAuthenticationFeature).Assembly];
            endpointOptions.Filter = endpoint => endpoint.Namespace == "Elsa.ExternalAuthentication.Endpoints.Previews";
        });
        builder.Services.AddAuthorization();
        builder.Services.AddRateLimiter(_ => { });
        builder.Services.AddSingleton(previews);
        var tenantAccessor = Substitute.For<ITenantAccessor>();
        tenantAccessor.TenantId.Returns("tenant-a");
        builder.Services.AddSingleton(tenantAccessor);

        _app = builder.Build();
        _app.UseAuthorization();
        _app.UseFastEndpoints();
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        EndpointSecurityOptions.SecurityIsEnabled = _wasSecurityEnabled;
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task AuthorizeReturnsProviderRedirectAndConsumedHandleReturnsGone()
    {
        var firstResponse = await _client!.GetAsync($"/external-authentication/previews/{PreviewHandle}/authorize");
        var secondResponse = await _client.GetAsync($"/external-authentication/previews/{PreviewHandle}/authorize");

        Assert.Equal(HttpStatusCode.Found, firstResponse.StatusCode);
        Assert.Equal(ProviderAuthorizationUri, firstResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.Gone, secondResponse.StatusCode);
    }

    [Fact]
    public async Task MissingPreviewResultReturnsNotFound()
    {
        var response = await _client!.GetAsync("/external-authentication/previews/missing-handle");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
