using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Features;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Notifications;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Elsa.Identity.Contracts;
using Elsa.Mediator.Contracts;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.ExternalAuthentication.IntegrationTests.Connections;

public class ConnectionManagementTests : IAsyncLifetime
{
    private WebApplication? _app;
    private HttpClient? _client;
    private bool _wasSecurityEnabled;
    private TestConnectionRegistry _registry = null!;
    private InMemoryIdentityProviderConnectionStore _store = null!;
    private InMemoryConnectionRegistryVersionStore _registryVersions = null!;
    private InMemoryConnectionObservationStore _observations = null!;
    private TestAdapterSettingsMigrationService _settingsMigrations = null!;
    private INotificationSender _notifications = null!;
    private bool _unsafePermissionGranted = true;
    private string _tenantId = "tenant-a";

    public async Task InitializeAsync()
    {
        _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = false;
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddFastEndpoints(options =>
        {
            options.Assemblies = [typeof(ExternalAuthenticationFeature).Assembly];
            options.Filter = endpoint => endpoint.Namespace == "Elsa.ExternalAuthentication.Endpoints.Connections";
        });
        builder.Services.AddAuthorization();
        builder.Services.Configure<ExternalAuthenticationOptions>(options =>
        {
            options.EnableDatabaseConnections = true;
            options.AllowedAdapterTypes = [];
            options.AllowedUnlinkedIdentityPolicyTypes = [];
            options.AllowedPermissionGrantSourceTypes = [];
        });
        _store = new InMemoryIdentityProviderConnectionStore();
        _registryVersions = new InMemoryConnectionRegistryVersionStore();
        _observations = new InMemoryConnectionObservationStore();
        _registry = new TestConnectionRegistry(_store);
        builder.Services.AddSingleton<IIdentityProviderConnectionStore>(_store);
        builder.Services.AddSingleton<IIdentityProviderConnectionRegistry>(_registry);
        builder.Services.AddSingleton<IConnectionRegistryVersionStore>(_registryVersions);
        builder.Services.AddSingleton<IConnectionObservationStore>(_observations);
        builder.Services.AddSingleton<ConnectionRevisionCalculator>();
        builder.Services.AddSingleton<IExternalAuthenticationAdapterRegistry>(new TestAdapterRegistry());
        _settingsMigrations = new TestAdapterSettingsMigrationService();
        builder.Services.AddSingleton<IAdapterSettingsMigrationService>(_settingsMigrations);
        builder.Services.AddSingleton(Substitute.For<IUnlinkedIdentityPolicyRegistry>());
        builder.Services.AddScoped(_ => Substitute.For<IPermissionGrantSourceRegistry>());
        builder.Services.AddSingleton<IPermissionDelegationAuthorizer>(Substitute.For<IPermissionDelegationAuthorizer>());
        _notifications = Substitute.For<INotificationSender>();
        builder.Services.AddSingleton(_notifications);
        builder.Services.AddSingleton<ISystemClock, SystemClock>();
        var tenant = Substitute.For<ITenantAccessor>();
        tenant.TenantId.Returns(_ => _tenantId);
        builder.Services.AddSingleton(tenant);
        builder.Services.AddScoped<IdentityProviderConnectionManagementService>();
        _app = builder.Build();
        _app.Use(async (context, next) =>
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(PermissionNames.ClaimType, _unsafePermissionGranted ? PermissionNames.All : ExternalAuthenticationPermissions.ConnectionsUpdate)], "test"));
            await next(context);
        });
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
    public async Task DatabaseConnectionLifecycleUsesEtagsAndPreservesItsIdentity()
    {
        var create = await _client!.PostAsJsonAsync("/external-authentication/connections", CreateRequest("contoso"));
        var created = await create.Content.ReadFromJsonAsync<ConnectionDocument>();

        Assert.True(create.StatusCode == HttpStatusCode.Created, await create.Content.ReadAsStringAsync());
        Assert.Equal("\"1\"", create.Headers.ETag?.Tag);
        var createdDocument = Assert.IsType<ConnectionDocument>(created);

        var update = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{createdDocument.Id}") { Content = JsonContent.Create(CreateRequest("contoso-renamed", displayName: "Updated")) };
        update.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        var updated = await _client!.SendAsync(update);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("\"2\"", updated.Headers.ETag?.Tag);

        var stale = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{createdDocument.Id}") { Content = JsonContent.Create(CreateRequest("contoso-stale")) };
        stale.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await _client.SendAsync(stale)).StatusCode);

        var enable = new HttpRequestMessage(HttpMethod.Post, $"/external-authentication/connections/{createdDocument.Id}/enable");
        enable.Headers.TryAddWithoutValidation("If-Match", "\"2\"");
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(enable)).StatusCode);

        var archive = new HttpRequestMessage(HttpMethod.Delete, $"/external-authentication/connections/{createdDocument.Id}");
        archive.Headers.TryAddWithoutValidation("If-Match", "\"3\"");
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(archive)).StatusCode);

        var restore = new HttpRequestMessage(HttpMethod.Post, $"/external-authentication/connections/{createdDocument.Id}/restore");
        restore.Headers.TryAddWithoutValidation("If-Match", "\"4\"");
        var restored = await _client.SendAsync(restore);
        var restoredDocument = await restored.Content.ReadFromJsonAsync<ConnectionDocument>();
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        var restoredConnection = Assert.IsType<ConnectionDocument>(restoredDocument);
        Assert.Equal(createdDocument.Id, restoredConnection.Id);
        Assert.False(restoredConnection.EnabledIntent);
    }

    [Fact]
    public async Task ConfigurationConnectionIsReadOnlyAndBlocksSameScopeKeyCreation()
    {
        _registry.ConfigurationConnection = new IdentityProviderConnection
        {
            Id = "configuration-contoso",
            TenantId = "tenant-a",
            Key = "contoso",
            AdapterType = "test",
            AdapterSettingsVersion = 1,
            AdapterSettings = JsonDocument.Parse("{}").RootElement.Clone(),
            DisplayName = "Configuration Contoso",
            ClaimProjection = ClaimProjection.Empty,
            MaterialRevision = "m-config",
            Revision = 1
        };

        var create = await _client!.PostAsJsonAsync("/external-authentication/connections", CreateRequest("contoso"));
        Assert.Equal(HttpStatusCode.Conflict, create.StatusCode);

        var update = new HttpRequestMessage(HttpMethod.Put, "/external-authentication/connections/configuration-contoso") { Content = JsonContent.Create(CreateRequest("contoso")) };
        update.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client!.SendAsync(update)).StatusCode);

        var lifecycle = new HttpRequestMessage(HttpMethod.Post, "/external-authentication/connections/configuration-contoso/disable");
        lifecycle.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(lifecycle)).StatusCode);

        var secret = new HttpRequestMessage(HttpMethod.Put, "/external-authentication/connections/configuration-contoso/secret-bindings/clientSecret") { Content = JsonContent.Create(new { resolverType = "test", reference = "secret" }) };
        secret.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(secret)).StatusCode);
    }

    [Fact]
    public async Task TenantCannotCreateOrMutateConnectionsOutsideItsExactScope()
    {
        var client = _client!;
        foreach (var scope in new[] { new { kind = "host", tenantId = (string?)null }, new { kind = "default", tenantId = (string?)null }, new { kind = "tenant", tenantId = (string?)"tenant-b" } })
        {
            var response = await _client!.PostAsJsonAsync("/external-authentication/connections", CreateRequest("scope-" + scope.kind, scope));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        var host = DatabaseConnection("host-connection", ConnectionScope.HostTenantId, "host-connection");
        await _store.CreateAsync(host);
        var update = new HttpRequestMessage(HttpMethod.Put, "/external-authentication/connections/host-connection") { Content = JsonContent.Create(CreateRequest("host-updated")) };
        update.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(update)).StatusCode);

        var lifecycle = new HttpRequestMessage(HttpMethod.Post, "/external-authentication/connections/host-connection/disable");
        lifecycle.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(lifecycle)).StatusCode);

        var secret = new HttpRequestMessage(HttpMethod.Put, "/external-authentication/connections/host-connection/secret-bindings/clientSecret") { Content = JsonContent.Create(new { resolverType = "test", reference = "secret" }) };
        secret.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(secret)).StatusCode);

        await _store.CreateAsync(DatabaseConnection("tenant-inherited-key", "tenant-a", "tenant-inherited-key"));
        _tenantId = ConnectionScope.HostTenantId;
        var hostCollision = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("tenant-inherited-key", new { kind = "host", tenantId = (string?)null }));
        Assert.Equal(HttpStatusCode.Conflict, hostCollision.StatusCode);
    }

    [Fact]
    public async Task ListSupportsDeterministicPagingFiltersAndStaleObservations()
    {
        var client = _client!;
        await _store.CreateAsync(DatabaseConnection("list-a", "tenant-a", "alpha", 1));
        await _store.CreateAsync(DatabaseConnection("list-b", "tenant-a", "bravo", 2));
        await _store.CreateAsync(DatabaseConnection("list-c", "tenant-a", "charlie", 3));
        await _store.CreateAsync(DatabaseConnection("other-tenant", "tenant-b", "not-enumerable", 4));
        await _observations.SaveLatestAsync(new ConnectionObservation("list-a", "old-material", DateTimeOffset.UtcNow, ConnectionObservationStatus.Succeeded, "connectivity", TimeSpan.Zero, "OK", [], "test"));

        var first = await client.GetFromJsonAsync<ListDocument>("/external-authentication/connections?source=database&valid=true&shadowed=false&pageSize=1");
        var firstPage = Assert.IsType<ListDocument>(first);
        var firstConnection = Assert.Single(firstPage.Items);
        Assert.Equal("alpha", firstConnection.Key);
        Assert.True(firstConnection.LatestObservation!.IsStale);
        Assert.NotNull(firstPage.NextCursor);

        var detail = await client.GetFromJsonAsync<ListConnectionDocument>("/external-authentication/connections/list-a");
        Assert.True(Assert.IsType<ListConnectionDocument>(detail).LatestObservation!.IsStale);

        var second = await client.GetFromJsonAsync<ListDocument>($"/external-authentication/connections?source=database&valid=true&shadowed=false&pageSize=1&cursor={Uri.EscapeDataString(firstPage.NextCursor!)}");
        var secondPage = Assert.IsType<ListDocument>(second);
        Assert.Equal("bravo", Assert.Single(secondPage.Items).Key);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/external-authentication/connections?source=unknown")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/external-authentication/connections?cursor=not-a-cursor")).StatusCode);
    }

    [Fact]
    public async Task DraftMayBeIncompleteButEnableRequiresAdapterValidationAndMigration()
    {
        var client = _client!;
        var versionBefore = await _registryVersions.GetVersionAsync();
        var create = await _client!.PostAsJsonAsync("/external-authentication/connections", CreateRequest("draft", settings: new { }));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var draft = Assert.IsType<ConnectionDocument>(await create.Content.ReadFromJsonAsync<ConnectionDocument>());
        Assert.Equal(2, draft.AdapterSettingsVersion);
        Assert.False(await _registryVersions.IsCurrentAsync(versionBefore));

        var enable = new HttpRequestMessage(HttpMethod.Post, $"/external-authentication/connections/{draft.Id}/enable");
        enable.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(enable)).StatusCode);

        var future = await client.PostAsJsonAsync("/external-authentication/connections", new { key = "future", scope = new { kind = "tenant", tenantId = "tenant-a" }, adapterType = "test", adapterSettingsVersion = 3, adapterSettings = new { valid = true }, displayName = "Future", claimProjection = new { }, upstreamLogoutMode = "disabled" });
        Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);
        Assert.Contains("migration_unavailable", await future.Content.ReadAsStringAsync());

        var secretInSettings = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("secret-in-settings", settings: new { valid = true, clientSecret = "not-allowed" }));
        Assert.Equal(HttpStatusCode.BadRequest, secretInSettings.StatusCode);
        Assert.Contains("secret_binding_required", await secretInSettings.Content.ReadAsStringAsync());

        _settingsMigrations.CanMigrateVersionOne = false;
        var missing = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("missing-migration"));
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Contains("migration_unavailable", await missing.Content.ReadAsStringAsync());

        _settingsMigrations.CanMigrateVersionOne = true;
        var uppercaseKey = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("UpperCase"));
        Assert.Equal(HttpStatusCode.BadRequest, uppercaseKey.StatusCode);
    }

    [Fact]
    public async Task ExistingUnsafeSettingsRemainManageableWithoutUnsafeConfirmation()
    {
        var client = _client!;
        var create = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("unsafe", settings: new { valid = true, unsafeMode = true }, confirmUnsafeSettings: true));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var connection = Assert.IsType<ConnectionDocument>(await create.Content.ReadFromJsonAsync<ConnectionDocument>());

        _unsafePermissionGranted = false;
        var safeSettingsUpdate = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connection.Id}") { Content = JsonContent.Create(CreateRequest("unsafe", settings: new { valid = true, unsafeMode = true, label = "changed" })) };
        safeSettingsUpdate.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(safeSettingsUpdate)).StatusCode);

        var validate = await client.PostAsync($"/external-authentication/connections/{connection.Id}/validate", null);
        Assert.Equal(HttpStatusCode.OK, validate.StatusCode);
        Assert.Contains("\"valid\":true", await validate.Content.ReadAsStringAsync());

        var secret = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connection.Id}/secret-bindings/clientSecret") { Content = JsonContent.Create(new { resolverType = "test", reference = "secret" }) };
        secret.Headers.TryAddWithoutValidation("If-Match", "\"2\"");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(secret)).StatusCode);
        await _notifications.Received().SendAsync(Arg.Is<IdentityProviderConnectionSecretBindingChanged>(x => x.FieldName == "clientSecret" && x.ResolverType == "test" && !x.IsConfigured), Arg.Any<CancellationToken>());
    }

    private static object CreateRequest(string key, object? scope = null, string displayName = "Contoso", object? settings = null, bool confirmUnsafeSettings = false) => new
    {
        key,
        scope = scope ?? new { kind = "tenant", tenantId = "tenant-a" },
        adapterType = "test",
        adapterSettingsVersion = 1,
        adapterSettings = settings ?? new { valid = true },
        displayName,
        order = 10,
        claimProjection = new { allowedClaimTypes = Array.Empty<string>(), redactedClaimTypes = Array.Empty<string>(), maximumClaimCount = 0, maximumValueLength = 0, maximumTotalBytes = 0 },
        upstreamLogoutMode = "disabled",
        confirmUnsafeSettings
    };

    private sealed class ConnectionDocument
    {
        public string Id { get; set; } = null!;
        public bool EnabledIntent { get; set; }
        public int AdapterSettingsVersion { get; set; }
    }

    private sealed class ListDocument
    {
        public List<ListConnectionDocument> Items { get; set; } = [];
        public string? NextCursor { get; set; }
    }

    private sealed class ListConnectionDocument
    {
        public string Key { get; set; } = null!;
        public ObservationDocument? LatestObservation { get; set; }
    }

    private sealed class ObservationDocument
    {
        public bool IsStale { get; set; }
    }

    private static IdentityProviderConnection DatabaseConnection(string id, string tenantId, string key, int order = 0) => new()
    {
        Id = id,
        TenantId = tenantId,
        Key = key,
        AdapterType = "test",
        AdapterSettingsVersion = 2,
        AdapterSettings = JsonDocument.Parse("{\"valid\":true}").RootElement.Clone(),
        DisplayName = key,
        DisplayOrder = order,
        ClaimProjection = ClaimProjection.Empty,
        MaterialRevision = "material-" + id,
        Revision = 1
    };

    private sealed class TestAdapterRegistry : IExternalAuthenticationAdapterRegistry
    {
        private readonly IExternalAuthenticationAdapter _adapter = new TestAdapter();
        public IReadOnlyCollection<ExternalAuthenticationAdapterDescriptor> ListDescriptors() => [_adapter.Describe()];
        public bool TryGet(string type, out IExternalAuthenticationAdapter adapter)
        {
            adapter = _adapter;
            return string.Equals(type, _adapter.Type, StringComparison.Ordinal);
        }
    }

    private sealed class TestAdapter : IExternalAuthenticationAdapter
    {
        public string Type => "test";
        public ExternalAuthenticationAdapterDescriptor Describe() => new(Type, "Test", "Test adapter", 2,
        [
            new SettingFieldDescriptor("clientSecret", "Client secret", "Secret", "secret", false, "secret", null, [], new SettingFieldValidation(), true, false, null, null, true),
            new SettingFieldDescriptor("unsafeMode", "Unsafe mode", "Unsafe", "boolean", false, "toggle", null, [], new SettingFieldValidation(), false, true, null, null, false)
        ], new(false, false, false), null);
        public ValueTask<ConnectionValidationResult> ValidateAsync(ConnectionValidationContext context, CancellationToken cancellationToken = default)
        {
            var settings = context.Connection.Connection.AdapterSettings;
            var valid = settings.ValueKind == JsonValueKind.Object && settings.TryGetProperty("valid", out var value) && value.ValueKind == JsonValueKind.True;
            return ValueTask.FromResult(valid
                ? new ConnectionValidationResult(true, [], [])
                : new ConnectionValidationResult(false, [new ConnectionValidationError("adapterSettings.valid", "required", "The test adapter requires valid=true.")], []));
        }
        public ValueTask<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync(ExternalAuthorizationContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalAuthenticationResult> AuthenticateCallbackAsync(ExternalCallbackContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ConnectionTestResult> TestAsync(ConnectionTestContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalLogoutRequest?> CreateLogoutRequestAsync(ExternalLogoutContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestAdapterSettingsMigrationService : IAdapterSettingsMigrationService
    {
        public bool CanMigrateVersionOne { get; set; } = true;

        public ValueTask<AdapterSettingsMigrationResult> MigrateAsync(string adapterType, int settingsVersion, JsonElement settings, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(adapterType, "test", StringComparison.Ordinal) || settingsVersion is < 1 or > 2 || (settingsVersion == 1 && !CanMigrateVersionOne))
                throw new InvalidOperationException("No compatible settings migration is available.");

            return ValueTask.FromResult(new AdapterSettingsMigrationResult(2, settings.Clone(), settingsVersion == 1));
        }
    }

    private sealed class TestConnectionRegistry(IIdentityProviderConnectionStore store) : IIdentityProviderConnectionRegistry
    {
        public IdentityProviderConnection? ConfigurationConnection { get; set; }

        public async ValueTask<EffectiveConnectionRegistry> GetAsync(string targetTenantId, CancellationToken cancellationToken = default)
        {
            var rows = await store.FindAsync(new ConnectionFilter(), cancellationToken);
            var database = rows.Items.Where(x => x.TenantId == targetTenantId || x.TenantId == ConnectionScope.HostTenantId)
                .Select(x => new EffectiveIdentityProviderConnection(x, ConnectionSourceOwnership.Database, ToScope(x.TenantId), ConnectionValidity.Unknown, false, "database"));
            IEnumerable<EffectiveIdentityProviderConnection> configuration = ConfigurationConnection is not null && (ConfigurationConnection.TenantId == targetTenantId || ConfigurationConnection.TenantId == ConnectionScope.HostTenantId)
                ? [new EffectiveIdentityProviderConnection(ConfigurationConnection, ConnectionSourceOwnership.Configuration, ToScope(ConfigurationConnection.TenantId), ConnectionValidity.Unknown, false, "configuration")]
                : Array.Empty<EffectiveIdentityProviderConnection>();
            var connections = configuration.Concat(database).ToArray();
            return new EffectiveConnectionRegistry(connections, [], "test");
        }

        public async ValueTask<EffectiveIdentityProviderConnection?> FindByKeyAsync(string targetTenantId, string key, CancellationToken cancellationToken = default) => (await GetAsync(targetTenantId, cancellationToken)).Connections.FirstOrDefault(x => string.Equals(x.Connection.Key, key, StringComparison.Ordinal));
        public async ValueTask<EffectiveIdentityProviderConnection?> FindByIdAsync(string targetTenantId, string connectionId, CancellationToken cancellationToken = default) => (await GetAsync(targetTenantId, cancellationToken)).Connections.FirstOrDefault(x => string.Equals(x.Connection.Id, connectionId, StringComparison.Ordinal));
        private static ConnectionScope ToScope(string tenantId) => tenantId == ConnectionScope.HostTenantId ? ConnectionScope.Host : tenantId.Length == 0 ? ConnectionScope.DefaultTenant : new ConnectionScope(ConnectionScopeKind.Tenant, tenantId);
    }
}
