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
using Microsoft.Extensions.Options;
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
    private TestAdapter _adapter = null!;
    private TestRoleAuthorizationService _roleAuthorizationService = null!;
    private TestManagedSecretBindingWriter _managedSecretWriter = null!;
    private IExternalAuthenticationSessionStore _sessions = null!;
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
            options.AllowedExternalUserMatcherTypes = ["allowed-matcher"];
            options.AllowedPermissionGrantSourceTypes = [];
            options.UnlinkedIdentityPolicy.AllowDatabaseConnectionOverride = true;
            options.FinalLoginPathGuard.IsEnabled = false;
            options.Redirects.ExternalCallbackBaseUri = new Uri("https://elsa.example/elsa/api/");
        });
        _store = new InMemoryIdentityProviderConnectionStore();
        _registryVersions = new InMemoryConnectionRegistryVersionStore();
        _observations = new InMemoryConnectionObservationStore();
        _registry = new TestConnectionRegistry(_store);
        builder.Services.AddSingleton<IIdentityProviderConnectionStore>(_store);
        builder.Services.AddSingleton<IIdentityProviderConnectionRegistry>(_registry);
        builder.Services.AddSingleton<FinalLoginPathGuard>();
        builder.Services.AddSingleton<IConnectionRegistryVersionStore>(_registryVersions);
        builder.Services.AddSingleton<IConnectionObservationStore>(_observations);
        builder.Services.AddSingleton<ConnectionRevisionCalculator>();
        _adapter = new TestAdapter();
        builder.Services.AddSingleton<IExternalAuthenticationAdapterRegistry>(new TestAdapterRegistry(_adapter));
        _settingsMigrations = new TestAdapterSettingsMigrationService();
        builder.Services.AddSingleton<IAdapterSettingsMigrationService>(_settingsMigrations);
        builder.Services.AddSingleton<IUnlinkedIdentityPolicyRegistry>(new TestUnlinkedIdentityPolicyRegistry());
        builder.Services.AddSingleton<IExternalUserMatcherRegistry>(new TestExternalUserMatcherRegistry("allowed-matcher", "disallowed-matcher"));
        builder.Services.AddScoped(_ => Substitute.For<IPermissionGrantSourceRegistry>());
        builder.Services.AddSingleton<IPermissionDelegationAuthorizer>(Substitute.For<IPermissionDelegationAuthorizer>());
        _roleAuthorizationService = new TestRoleAuthorizationService();
        builder.Services.AddSingleton<IRoleAuthorizationService>(_roleAuthorizationService);
        _notifications = Substitute.For<INotificationSender>();
        builder.Services.AddSingleton(_notifications);
        builder.Services.AddSingleton<ISystemClock, SystemClock>();
        _sessions = Substitute.For<IExternalAuthenticationSessionStore>();
        builder.Services.AddSingleton(_sessions);
        _managedSecretWriter = new TestManagedSecretBindingWriter();
        builder.Services.AddSingleton<IManagedSecretBindingWriter>(_managedSecretWriter);
        builder.Services.AddSingleton<ISecretBindingResolver>(new TestSecretBindingResolver());
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
        Assert.Equal("https://elsa.example/elsa/api/external-authentication/callback/contoso", createdDocument.CallbackUri);
        Assert.Equal($"https://elsa.example/elsa/api/external-authentication/previews/callback/{createdDocument.Id}", createdDocument.PreviewCallbackUri);

        var immutableKeyUpdate = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{createdDocument.Id}") { Content = JsonContent.Create(CreateRequest("contoso-renamed", displayName: "Updated")) };
        immutableKeyUpdate.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        var immutableKeyResponse = await _client!.SendAsync(immutableKeyUpdate);
        Assert.Equal(HttpStatusCode.Conflict, immutableKeyResponse.StatusCode);
        var immutableKeyContent = await immutableKeyResponse.Content.ReadAsStringAsync();
        Assert.Contains("connection_key_immutable", immutableKeyContent);
        using (var errorDocument = JsonDocument.Parse(immutableKeyContent))
        {
            var correlationId = errorDocument.RootElement.GetProperty("correlationId").GetString();
            Assert.Matches("^[A-Za-z0-9_-]{1,128}$", correlationId);
        }

        var update = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{createdDocument.Id}") { Content = JsonContent.Create(CreateRequest("contoso", displayName: "Updated")) };
        update.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        var updated = await _client!.SendAsync(update);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("\"2\"", updated.Headers.ETag?.Tag);

        var validate = await _client.PostAsync($"/external-authentication/connections/{createdDocument.Id}/validate", null);
        Assert.Equal(HttpStatusCode.OK, validate.StatusCode);
        Assert.Contains("\"valid\":true", await validate.Content.ReadAsStringAsync());

        var stale = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{createdDocument.Id}") { Content = JsonContent.Create(CreateRequest("contoso", displayName: "Stale")) };
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
    public async Task ValidateRequiresCompleteConfigurationAndReturnsMissingSecretDetails()
    {
        _adapter.RequiresClientSecret = true;
        var create = await _client!.PostAsJsonAsync("/external-authentication/connections", CreateRequest("missing-secret"));
        var connection = Assert.IsType<ConnectionDocument>(await create.Content.ReadFromJsonAsync<ConnectionDocument>());

        var validate = await _client!.PostAsync($"/external-authentication/connections/{connection.Id}/validate", null);
        var validation = JsonDocument.Parse(await validate.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, validate.StatusCode);
        Assert.False(validation.GetProperty("valid").GetBoolean());
        var error = Assert.Single(validation.GetProperty("errors").EnumerateArray());
        Assert.Equal("secretBindings.clientSecret", error.GetProperty("field").GetString());
        Assert.Equal("required", error.GetProperty("code").GetString());
        Assert.Equal("A required secret binding is missing.", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ConnectionResponseEmitsCanonicalUpstreamLogoutModeString()
    {
        var response = await _client!.PostAsJsonAsync(
            "/external-authentication/connections",
            CreateRequest("user-choice-logout", upstreamLogoutMode: "user-choice"));
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(JsonValueKind.String, body.RootElement.GetProperty("upstreamLogoutMode").ValueKind);
        Assert.Equal("user-choice", body.RootElement.GetProperty("upstreamLogoutMode").GetString());
    }

    [Fact]
    public async Task ConfigurationConnectionIsReadOnlyAndBlocksSameScopeKeyCreation()
    {
        _registry.ConfigurationConnection = ConfigurationConnection("contoso");

        var create = await _client!.PostAsJsonAsync("/external-authentication/connections", CreateRequest("contoso"));
        Assert.Equal(HttpStatusCode.Conflict, create.StatusCode);

        var update = new HttpRequestMessage(HttpMethod.Put, "/external-authentication/connections/configuration-contoso") { Content = JsonContent.Create(CreateRequest("contoso")) };
        update.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client!.SendAsync(update)).StatusCode);

        var lifecycle = new HttpRequestMessage(HttpMethod.Post, "/external-authentication/connections/configuration-contoso/disable");
        lifecycle.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(lifecycle)).StatusCode);

        var secret = new HttpRequestMessage(HttpMethod.Put, "/external-authentication/connections/configuration-contoso/secret-bindings/clientSecret/managed") { Content = JsonContent.Create(new { resolverType = "test-managed", value = "secret" }) };
        secret.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(secret)).StatusCode);
    }

    [Fact]
    public async Task ShadowedDatabaseConnectionAdvertisesPromotionCapabilityOnlyWhenAllowedAndActive()
    {
        const string connectionId = "database-contoso";
        _registry.ConfigurationConnection = ConfigurationConnection("contoso");
        await _store.CreateAsync(DatabaseConnection(connectionId, ConnectionScope.HostTenantId, "contoso"));

        var shadowedDatabase = await GetConnectionResponseAsync(connectionId);
        Assert.False(shadowedDatabase.CanPromoteToConfigurationOverride);
        Assert.Equal("configuration-contoso", shadowedDatabase.ShadowedBy?.Id);
        Assert.Equal(connectionId, Assert.Single((await GetConnectionResponseAsync("configuration-contoso")).Shadows).Id);

        _app!.Services.GetRequiredService<IOptions<ExternalAuthenticationOptions>>().Value.AllowConfigurationConnectionOverrides = true;
        Assert.True((await GetConnectionResponseAsync(connectionId)).CanPromoteToConfigurationOverride);

        var connection = Assert.IsType<IdentityProviderConnection>(await _store.FindByIdAsync(connectionId));
        connection.OverridesConfigurationConnection = true;
        await _store.UpdateAsync(connection, connection.Revision);
        Assert.False((await GetConnectionResponseAsync(connectionId)).CanPromoteToConfigurationOverride);

        connection = Assert.IsType<IdentityProviderConnection>(await _store.FindByIdAsync(connectionId));
        connection.ArchivedAt = DateTimeOffset.UtcNow;
        await _store.UpdateAsync(connection, connection.Revision);
        Assert.False((await GetConnectionResponseAsync(connectionId)).CanPromoteToConfigurationOverride);
    }

    [Fact]
    public async Task PromotingShadowedConnectionUpdatesTheExistingRecordAndPreservesLifecycleAndSecretBindings()
    {
        const string connectionId = "database-contoso";
        _registry.ConfigurationConnection = ConfigurationConnection("contoso", isEnabled: true);
        var databaseConnection = DatabaseConnection(connectionId, ConnectionScope.HostTenantId, "contoso");
        databaseConnection.IsEnabled = true;
        databaseConnection.SecretBindings["clientSecret"] = new SecretBinding("test-managed", "preserved-secret");
        await _store.CreateAsync(databaseConnection);

        var denied = await UpdateConnectionAsync(connectionId, 1, CreateRequest("contoso", overridesConfigurationConnection: true));
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        Assert.False(Assert.IsType<IdentityProviderConnection>(await _store.FindByIdAsync(connectionId)).OverridesConfigurationConnection);

        _app!.Services.GetRequiredService<IOptions<ExternalAuthenticationOptions>>().Value.AllowConfigurationConnectionOverrides = true;
        var promoted = await UpdateConnectionAsync(connectionId, 1, CreateRequest("contoso", overridesConfigurationConnection: true));
        var promotedDocument = Assert.IsType<ConnectionDocument>(await promoted.Content.ReadFromJsonAsync<ConnectionDocument>());

        Assert.Equal(HttpStatusCode.OK, promoted.StatusCode);
        Assert.Equal(connectionId, promotedDocument.Id);
        Assert.True(promotedDocument.EnabledIntent);
        var persisted = Assert.IsType<IdentityProviderConnection>(await _store.FindByIdAsync(connectionId));
        Assert.True(persisted.OverridesConfigurationConnection);
        Assert.True(persisted.IsEnabled);
        Assert.Equal("preserved-secret", persisted.SecretBindings["clientSecret"].Reference);

        var effective = await _registry.GetAsync(_tenantId);
        Assert.True(effective.Connections.Single(x => x.Connection.Id == "configuration-contoso").IsShadowed);
        Assert.False(effective.Connections.Single(x => x.Connection.Id == connectionId).IsShadowed);
    }

    [Fact]
    public async Task PromotionOfDisabledShadowedConnectionIsBlockedWhenItWouldRemoveTheFinalLoginPath()
    {
        const string connectionId = "database-contoso";
        _registry.ConfigurationConnection = ConfigurationConnection("contoso", isEnabled: true);
        await _store.CreateAsync(DatabaseConnection(connectionId, ConnectionScope.HostTenantId, "contoso"));
        var options = _app!.Services.GetRequiredService<IOptions<ExternalAuthenticationOptions>>().Value;
        options.AllowConfigurationConnectionOverrides = true;
        options.LocalLogin.IsEnabled = false;
        options.FinalLoginPathGuard.IsEnabled = true;
        options.FinalLoginPathGuard.RequireRecoveryMethod = true;
        options.FinalLoginPathGuard.HasBreakGlassAuthentication = false;

        var promotion = await UpdateConnectionAsync(connectionId, 1, CreateRequest("contoso", overridesConfigurationConnection: true));

        Assert.Equal(HttpStatusCode.Conflict, promotion.StatusCode);
        Assert.Contains("final_login_path_guard", await promotion.Content.ReadAsStringAsync());
        Assert.False(Assert.IsType<IdentityProviderConnection>(await _store.FindByIdAsync(connectionId)).OverridesConfigurationConnection);
    }

    [Fact]
    public async Task ConnectionResponsesRedactDescriptorDeclaredSecretsInSettings()
    {
        var connection = DatabaseConnection("legacy-secret", ConnectionScope.HostTenantId, "legacy-secret");
        connection.AdapterSettings = JsonDocument.Parse("{\"valid\":true,\"clientSecret\":\"must-not-leave-the-server\"}").RootElement.Clone();
        await _store.CreateAsync(connection);

        var response = await _client!.GetAsync("/external-authentication/connections/legacy-secret");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("must-not-leave-the-server", body, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConnectionResponsesOmitSettingsWhenAdapterIsUnavailable()
    {
        var connection = DatabaseConnection("removed-adapter", ConnectionScope.HostTenantId, "removed-adapter");
        connection.AdapterType = "removed";
        connection.AdapterSettings = JsonDocument.Parse("{\"clientSecret\":\"must-not-leave-the-server\",\"issuer\":\"https://issuer.example\"}").RootElement.Clone();
        await _store.CreateAsync(connection);

        var response = await _client!.GetAsync("/external-authentication/connections/removed-adapter");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("must-not-leave-the-server", body, StringComparison.Ordinal);
        Assert.DoesNotContain("issuer.example", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConnectionsAreManagedHostWideRegardlessOfCurrentTenant()
    {
        var client = _client!;
        foreach (var scope in new[] { new { kind = "default", tenantId = (string?)null }, new { kind = "tenant", tenantId = (string?)"tenant-b" } })
        {
            var response = await _client!.PostAsJsonAsync("/external-authentication/connections", CreateRequest("scope-" + scope.kind, scope));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("host_scope_required", await response.Content.ReadAsStringAsync());
        }

        var create = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("host-connection"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var host = Assert.IsType<ConnectionDocument>(await create.Content.ReadFromJsonAsync<ConnectionDocument>());

        var update = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{host.Id}") { Content = JsonContent.Create(CreateRequest("host-connection", displayName: "Updated")) };
        update.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(update)).StatusCode);

        var secret = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{host.Id}/secret-bindings/clientSecret/managed") { Content = JsonContent.Create(new { resolverType = "test-managed", value = "secret" }) };
        secret.Headers.TryAddWithoutValidation("If-Match", "\"2\"");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(secret)).StatusCode);

        await _store.CreateAsync(DatabaseConnection("legacy-tenant", "tenant-a", "legacy-tenant"));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/external-authentication/connections/legacy-tenant")).StatusCode);

        await _store.CreateAsync(DatabaseConnection("tenant-inherited-key", "tenant-a", "tenant-inherited-key"));
        var hostCollision = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("tenant-inherited-key", new { kind = "host", tenantId = (string?)null }));
        Assert.Equal(HttpStatusCode.Conflict, hostCollision.StatusCode);
    }

    [Fact]
    public async Task ListSupportsDeterministicPagingFiltersAndStaleObservations()
    {
        var client = _client!;
        await _store.CreateAsync(DatabaseConnection("list-a", ConnectionScope.HostTenantId, "alpha", 1));
        await _store.CreateAsync(DatabaseConnection("list-b", ConnectionScope.HostTenantId, "bravo", 2));
        await _store.CreateAsync(DatabaseConnection("list-c", ConnectionScope.HostTenantId, "charlie", 3));
        await _store.CreateAsync(DatabaseConnection("legacy-tenant", "tenant-b", "not-enumerable", 4));
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

        var future = await client.PostAsJsonAsync("/external-authentication/connections", new { key = "future", scope = new { kind = "host" }, adapterType = "test", adapterSettingsVersion = 3, adapterSettings = new { valid = true }, displayName = "Future", claimProjection = new { }, upstreamLogoutMode = "disabled" });
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

        var secret = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connection.Id}/secret-bindings/clientSecret/managed") { Content = JsonContent.Create(new { resolverType = "test-managed", value = "secret" }) };
        secret.Headers.TryAddWithoutValidation("If-Match", "\"2\"");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(secret)).StatusCode);
        await _notifications.Received().SendAsync(Arg.Is<IdentityProviderConnectionSecretBindingChanged>(x => x.FieldName == "clientSecret" && x.ResolverType == "test-managed" && !x.IsConfigured), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ManagedSecretReplacementCleansUpStagedMaterialWhenConnectionCasLoses()
    {
        var client = _client!;
        var create = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("managed-secret-race"));
        var connection = Assert.IsType<ConnectionDocument>(await create.Content.ReadFromJsonAsync<ConnectionDocument>());
        _managedSecretWriter.BeforeReturn = async () =>
        {
            var concurrent = Assert.IsType<IdentityProviderConnection>(await _store.FindByIdAsync(connection.Id));
            concurrent.DisplayName = "Concurrent update";
            Assert.IsType<ConnectionMutationResult.Updated>(await _store.UpdateAsync(concurrent, concurrent.Revision));
        };

        var replace = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connection.Id}/secret-bindings/clientSecret/managed")
        {
            Content = JsonContent.Create(new { resolverType = "test-managed", value = "replacement" })
        };
        replace.Headers.TryAddWithoutValidation("If-Match", "\"1\"");

        Assert.Equal(HttpStatusCode.PreconditionFailed, (await client.SendAsync(replace)).StatusCode);
        Assert.Single(_managedSecretWriter.RemovedReferences);
        Assert.Empty(Assert.IsType<IdentityProviderConnection>(await _store.FindByIdAsync(connection.Id)).SecretBindings);
    }

    [Fact]
    public async Task ManagedSecretReplacementCleansUpStagedMaterialWhenValidationThrows()
    {
        var create = await _client!.PostAsJsonAsync(
            "/external-authentication/connections",
            CreateRequest("managed-secret-exception", unlinkedPolicy: CreateMatcherPolicy("allowed-matcher", "create-user")));
        var connection = Assert.IsType<ConnectionDocument>(await create.Content.ReadFromJsonAsync<ConnectionDocument>());
        _managedSecretWriter.BeforeReturn = () =>
        {
            _roleAuthorizationService.ThrowOnAssignRoles = true;
            return Task.CompletedTask;
        };
        var replace = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connection.Id}/secret-bindings/clientSecret/managed")
        {
            Content = JsonContent.Create(new { resolverType = "test-managed", value = "replacement" })
        };
        replace.Headers.TryAddWithoutValidation("If-Match", "\"1\"");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _client!.SendAsync(replace));

        Assert.Equal(new[] { "staged-1" }, _managedSecretWriter.RemovedReferences);
        Assert.Empty(Assert.IsType<IdentityProviderConnection>(await _store.FindByIdAsync(connection.Id)).SecretBindings);
    }

    [Fact]
    public async Task DisablingWithSessionRevocationRequiresPermissionAndEmitsAggregateNotification()
    {
        var connection = DatabaseConnection("disable-with-revoke", ConnectionScope.HostTenantId, "disable-with-revoke");
        connection.IsEnabled = true;
        await _store.CreateAsync(connection);
        _sessions.RevokeActiveForConnectionAsync("disable-with-revoke", "connection_disabled", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(2);
        _unsafePermissionGranted = false;

        var forbidden = new HttpRequestMessage(HttpMethod.Post, "/external-authentication/connections/disable-with-revoke/disable?revokeActiveSessions=true");
        forbidden.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client!.SendAsync(forbidden)).StatusCode);
        await _sessions.DidNotReceive().RevokeActiveForConnectionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());

        _unsafePermissionGranted = true;
        var allowed = new HttpRequestMessage(HttpMethod.Post, "/external-authentication/connections/disable-with-revoke/disable?revokeActiveSessions=true");
        allowed.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(allowed)).StatusCode);
        await _notifications.Received().SendAsync(
            Arg.Is<ExternalAuthenticationConnectionSessionsRevoked>(x => x.SessionCount == 2 && x.Reason == "connection_disabled"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ManagedSecretReplacementRemainsPublishedWhenPostCommitNotificationFails()
    {
        var client = _client!;
        var create = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("managed-secret-notification"));
        var connection = Assert.IsType<ConnectionDocument>(await create.Content.ReadFromJsonAsync<ConnectionDocument>());
        _notifications
            .SendAsync(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException("Notification failure")));

        var replace = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connection.Id}/secret-bindings/clientSecret/managed")
        {
            Content = JsonContent.Create(new { resolverType = "test-managed", value = "replacement" })
        };
        replace.Headers.TryAddWithoutValidation("If-Match", "\"1\"");

        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(replace)).StatusCode);
        var persisted = Assert.IsType<IdentityProviderConnection>(await _store.FindByIdAsync(connection.Id));
        Assert.Equal("staged-1", persisted.SecretBindings["clientSecret"].Reference);
        Assert.Empty(_managedSecretWriter.RemovedReferences);
    }

    [Fact]
    public async Task ManagedSecretWriterMustStageAReferenceDistinctFromTheLiveBinding()
    {
        var client = _client!;
        var create = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("managed-secret-distinct"));
        var connection = Assert.IsType<ConnectionDocument>(await create.Content.ReadFromJsonAsync<ConnectionDocument>());
        var first = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connection.Id}/secret-bindings/clientSecret/managed")
        {
            Content = JsonContent.Create(new { resolverType = "test-managed", value = "first" })
        };
        first.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(first)).StatusCode);

        _managedSecretWriter.ReferenceToReturn = "staged-1";
        var invalid = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connection.Id}/secret-bindings/clientSecret/managed")
        {
            Content = JsonContent.Create(new { resolverType = "test-managed", value = "second" })
        };
        invalid.Headers.TryAddWithoutValidation("If-Match", "\"2\"");

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(invalid));
        Assert.Equal("staged-1", Assert.IsType<IdentityProviderConnection>(await _store.FindByIdAsync(connection.Id)).SecretBindings["clientSecret"].Reference);
        Assert.Empty(_managedSecretWriter.RemovedReferences);
    }

    [Fact]
    public async Task GeneralConnectionPayloadCannotInjectOrClearSecretBindings()
    {
        var client = _client!;
        var injectedCreate = await client.PostAsJsonAsync("/external-authentication/connections", new
        {
            key = "injected-secret",
            scope = new { kind = "host" },
            adapterType = "test",
            adapterSettingsVersion = 1,
            adapterSettings = new { valid = true },
            displayName = "Injected",
            secretBindings = new { clientSecret = new { resolverType = "configuration", reference = "ConnectionStrings:Production" } },
            claimProjection = new { },
            upstreamLogoutMode = "disabled"
        });
        Assert.Equal(HttpStatusCode.BadRequest, injectedCreate.StatusCode);
        Assert.Contains("secret_bindings_mutation_not_allowed", await injectedCreate.Content.ReadAsStringAsync());

        var create = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("cannot-clear-secret"));
        var connection = Assert.IsType<ConnectionDocument>(await create.Content.ReadFromJsonAsync<ConnectionDocument>());
        var clear = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connection.Id}")
        {
            Content = JsonContent.Create(new
            {
                key = "cannot-clear-secret",
                scope = new { kind = "host" },
                adapterType = "test",
                adapterSettingsVersion = 2,
                adapterSettings = new { valid = true },
                displayName = "Cannot clear",
                secretBindings = new { },
                claimProjection = new { },
                upstreamLogoutMode = "disabled"
            })
        };
        clear.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(clear)).StatusCode);
    }

    [Fact]
    public async Task MatcherPolicyRejectsAMatcherDisallowedByDeployment()
    {
        var response = await _client!.PostAsJsonAsync(
            "/external-authentication/connections",
            CreateRequest("disallowed-matcher", unlinkedPolicy: CreateMatcherPolicy("disallowed-matcher", "reject")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("validation_failed", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MatcherCreateUserFallbackRequiresRoleDelegation()
    {
        _roleAuthorizationService.CanAssignRoles = false;

        var response = await _client!.PostAsJsonAsync(
            "/external-authentication/connections",
            CreateRequest("matcher-roles", unlinkedPolicy: CreateMatcherPolicy("allowed-matcher", "create-user")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("validation_failed", await response.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "workflow-user" }, _roleAuthorizationService.LastRequestedRoleIds);
    }

    private static object CreateRequest(string key, object? scope = null, string displayName = "Contoso", object? settings = null, bool confirmUnsafeSettings = false, object? unlinkedPolicy = null, string upstreamLogoutMode = "disabled", bool overridesConfigurationConnection = false) => new
    {
        key,
        scope = scope ?? new { kind = "host" },
        adapterType = "test",
        adapterSettingsVersion = 1,
        adapterSettings = settings ?? new { valid = true },
        displayName,
        order = 10,
        claimProjection = new { allowedClaimTypes = Array.Empty<string>(), redactedClaimTypes = Array.Empty<string>(), maximumClaimCount = 0, maximumValueLength = 0, maximumTotalBytes = 0 },
        upstreamLogoutMode,
        confirmUnsafeSettings,
        overridesConfigurationConnection,
        unlinkedPolicy
    };

    private static PolicySelection CreateMatcherPolicy(string matcherType, string noMatchAction) => new(
        "match-user",
        1,
        JsonSerializer.SerializeToElement(new
        {
            matcher = new { type = matcherType, settingsVersion = 1, settings = new { } },
            noMatchAction,
            defaultRoleIds = new[] { "workflow-user" }
        }));

    private sealed class ConnectionDocument
    {
        public string Id { get; set; } = null!;
        public string? CallbackUri { get; set; }
        public string? PreviewCallbackUri { get; set; }
        public bool EnabledIntent { get; set; }
        public int AdapterSettingsVersion { get; set; }
        public bool CanPromoteToConfigurationOverride { get; set; }
        public ConnectionReferenceDocument? ShadowedBy { get; set; }
        public ICollection<ConnectionReferenceDocument> Shadows { get; set; } = [];
    }

    private sealed class ConnectionReferenceDocument
    {
        public string Id { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Source { get; set; } = null!;
    }

    private async Task<ConnectionDocument> GetConnectionResponseAsync(string connectionId)
    {
        var response = await _client!.GetAsync($"/external-authentication/connections/{connectionId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<ConnectionDocument>(await response.Content.ReadFromJsonAsync<ConnectionDocument>());
    }

    private async Task<HttpResponseMessage> UpdateConnectionAsync(string connectionId, long revision, object request)
    {
        var update = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connectionId}") { Content = JsonContent.Create(request) };
        update.Headers.TryAddWithoutValidation("If-Match", $"\"{revision}\"");
        return await _client!.SendAsync(update);
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

    private static IdentityProviderConnection ConfigurationConnection(string key, bool isEnabled = false) => new()
    {
        Id = "configuration-" + key,
        TenantId = ConnectionScope.HostTenantId,
        Key = key,
        AdapterType = "test",
        AdapterSettingsVersion = 1,
        AdapterSettings = JsonDocument.Parse("{}").RootElement.Clone(),
        DisplayName = "Configuration " + key,
        IsEnabled = isEnabled,
        ClaimProjection = ClaimProjection.Empty,
        MaterialRevision = "m-configuration-" + key,
        Revision = 1
    };

    private sealed class TestAdapterRegistry(IExternalAuthenticationAdapter registeredAdapter) : IExternalAuthenticationAdapterRegistry
    {
        public IReadOnlyCollection<ExternalAuthenticationAdapterDescriptor> ListDescriptors() => [registeredAdapter.Describe()];
        public bool TryGet(string type, out IExternalAuthenticationAdapter adapter)
        {
            adapter = registeredAdapter;
            return string.Equals(type, registeredAdapter.Type, StringComparison.Ordinal);
        }
    }

    private sealed class TestAdapter : IExternalAuthenticationAdapter
    {
        public string Type => "test";
        public bool RequiresClientSecret { get; set; }
        public ExternalAuthenticationAdapterDescriptor Describe() => new(Type, "Test", "Test adapter", 2,
        [
            new SettingFieldDescriptor("clientSecret", "Client secret", "Secret", "secret", RequiresClientSecret, "secret", null, [], new SettingFieldValidation(), true, false, null, null, true),
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

    private sealed class TestUnlinkedIdentityPolicyRegistry : IUnlinkedIdentityPolicyRegistry
    {
        private readonly IUnlinkedIdentityPolicy _matchUser = new TestUnlinkedIdentityPolicy("match-user");

        public IReadOnlyCollection<UnlinkedIdentityPolicyDescriptor> ListDescriptors() => [];
        public bool TryGet(string type, out IUnlinkedIdentityPolicy policy)
        {
            policy = _matchUser;
            return string.Equals(type, policy.Type, StringComparison.Ordinal);
        }
    }

    private sealed class TestUnlinkedIdentityPolicy(string type) : IUnlinkedIdentityPolicy
    {
        public string Type => type;
        public UnlinkedIdentityPolicyDescriptor Describe() => new(Type, Type, Type, 1, [], null);
        public ValueTask<UnlinkedIdentityDecision> EvaluateAsync(UnlinkedIdentityContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestExternalUserMatcherRegistry : IExternalUserMatcherRegistry
    {
        private readonly IReadOnlyDictionary<string, IExternalUserMatcher> _items;

        public TestExternalUserMatcherRegistry(params string[] types) => _items = types
            .Select(type => (IExternalUserMatcher)new TestExternalUserMatcher(type))
            .ToDictionary(x => x.Type, StringComparer.Ordinal);

        public IReadOnlyCollection<ExternalUserMatcherDescriptor> ListDescriptors() => _items.Values.Select(x => x.Describe()).ToArray();
        public bool TryGet(string type, out IExternalUserMatcher matcher) => _items.TryGetValue(type, out matcher!);
    }

    private sealed class TestExternalUserMatcher(string type) : IExternalUserMatcher
    {
        public string Type => type;
        public ExternalUserMatcherDescriptor Describe() => new(Type, Type, Type, 1, [], null);
        public ValueTask<ExternalUserMatchResult> MatchAsync(ExternalUserMatcherContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestRoleAuthorizationService : IRoleAuthorizationService
    {
        public bool CanAssignRoles { get; set; } = true;
        public bool ThrowOnAssignRoles { get; set; }
        public IReadOnlyCollection<string> LastRequestedRoleIds { get; private set; } = [];

        public Task<bool> CanAssignRolesAsync(ClaimsPrincipal user, IEnumerable<string>? roleIds, CancellationToken cancellationToken = default)
        {
            if (ThrowOnAssignRoles)
                throw new InvalidOperationException("Test role authorization failure.");
            LastRequestedRoleIds = (roleIds ?? []).ToArray();
            return Task.FromResult(CanAssignRoles);
        }

        public bool CanCreateRoleWithPermissions(ClaimsPrincipal user, IEnumerable<string>? permissions) => true;
        public bool CanMutateRole(ClaimsPrincipal user, Elsa.Identity.Entities.Role role, IEnumerable<string>? replacementPermissions = null) => true;
    }

    private sealed class TestManagedSecretBindingWriter : IManagedSecretBindingWriter
    {
        private int _sequence;

        public string ResolverType => "test-managed";
        public string DisplayName => "Test managed secrets";
        public Func<Task>? BeforeReturn { get; set; }
        public string? ReferenceToReturn { get; set; }
        public List<string> RemovedReferences { get; } = [];

        public async ValueTask<SecretBinding> StageAsync(ManagedSecretBindingWriteRequest request, CancellationToken cancellationToken = default)
        {
            if (BeforeReturn is not null)
                await BeforeReturn();
            var reference = ReferenceToReturn ?? $"staged-{Interlocked.Increment(ref _sequence)}";
            return new SecretBinding(ResolverType, reference, Ownership: SecretBindingOwnership.Managed);
        }

        public ValueTask RemoveAsync(SecretBinding binding, CancellationToken cancellationToken = default)
        {
            RemovedReferences.Add(binding.Reference);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestSecretBindingResolver : ISecretBindingResolver
    {
        public string Type => "test-managed";
        public ValueTask<SecretBindingState> GetStateAsync(SecretBinding binding, CancellationToken cancellationToken = default)
        {
            var isConfigured = string.Equals(binding.Reference, "preserved-secret", StringComparison.Ordinal);
            return ValueTask.FromResult(new SecretBindingState(isConfigured, isConfigured));
        }
        public ValueTask<ResolvedSecretBinding> ResolveAsync(SecretBinding binding, CancellationToken cancellationToken = default) => ValueTask.FromResult(new ResolvedSecretBinding(new SensitiveString("secret"), "test"));
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
            var candidates = configuration.Concat(database).ToArray();
            var connections = candidates
                .GroupBy(x => ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), StringComparer.Ordinal)
                .SelectMany(group =>
                {
                    var candidatesForKey = group.ToArray();
                    var preferred = candidatesForKey.FirstOrDefault(x => x.Ownership == ConnectionSourceOwnership.Database && x.Connection.OverridesConfigurationConnection && !x.Connection.ArchivedAt.HasValue)
                        ?? candidatesForKey.FirstOrDefault(x => x.Ownership == ConnectionSourceOwnership.Configuration)
                        ?? candidatesForKey[0];
                    var preferredReference = ToReference(preferred);
                    var shadowedReferences = candidatesForKey
                        .Where(candidate => !ReferenceEquals(candidate, preferred))
                        .Select(ToReference)
                        .ToArray();
                    return candidatesForKey.Select(candidate =>
                    {
                        var isShadowed = !ReferenceEquals(candidate, preferred);
                        return candidate with
                        {
                            IsShadowed = isShadowed,
                            ShadowedBy = isShadowed ? preferredReference : null,
                            Shadows = isShadowed ? [] : shadowedReferences
                        };
                    });
                })
                .ToArray();
            return new EffectiveConnectionRegistry(connections, [], "test");
        }

        public async ValueTask<EffectiveIdentityProviderConnection?> FindByKeyAsync(string targetTenantId, string key, CancellationToken cancellationToken = default) => (await GetAsync(targetTenantId, cancellationToken)).Connections.FirstOrDefault(x => string.Equals(x.Connection.Key, key, StringComparison.Ordinal));
        public async ValueTask<EffectiveIdentityProviderConnection?> FindByIdAsync(string targetTenantId, string connectionId, CancellationToken cancellationToken = default) => (await GetAsync(targetTenantId, cancellationToken)).Connections.FirstOrDefault(x => string.Equals(x.Connection.Id, connectionId, StringComparison.Ordinal));
        private static ConnectionScope ToScope(string tenantId) => tenantId == ConnectionScope.HostTenantId ? ConnectionScope.Host : tenantId.Length == 0 ? ConnectionScope.DefaultTenant : new ConnectionScope(ConnectionScopeKind.Tenant, tenantId);
        private static IdentityProviderConnectionReference ToReference(EffectiveIdentityProviderConnection connection) =>
            new(connection.Connection.Id, connection.Connection.DisplayName, connection.Ownership);
    }
}
