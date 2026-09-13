using Elsa.Authorization;
using Elsa.Extensions;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Notifications;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Elsa.Identity.Contracts;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Elsa.ExternalAuthentication.IntegrationTests.Fixtures;
using TUnit.AspNetCore;

namespace Elsa.ExternalAuthentication.IntegrationTests.Connections;

public class ConnectionManagementTests : WebApplicationTest<ConnectionManagementWebApplicationFactory, ExternalAuthenticationTestEntryPoint>
{
    private HttpClient? _client;
    private readonly TestAuthenticationState _authentication = new();
    private readonly TestConnectionRegistry _registry;
    private readonly InMemoryIdentityProviderConnectionStore _store;
    private readonly InMemoryConnectionRegistryVersionStore _registryVersions;
    private readonly InMemoryConnectionObservationStore _observations;
    private readonly TestAdapterSettingsMigrationService _settingsMigrations;
    private readonly TestAdapter _adapter;
    private readonly TestRoleAuthorizationService _roleAuthorizationService;
    private readonly TestManagedSecretBindingWriter _managedSecretWriter;
    private readonly IExternalAuthenticationSessionStore _sessions;
    private readonly INotificationSender _notifications;

    /// <summary>
    /// Overrides the acting principal's permissions for one test.
    /// </summary>
    /// <remarks>
    /// The default is all-or-nothing, which cannot express "may manage policies but may not decide default
    /// roles" -- the separation of duties #7977 is about. A test that needs that distinction sets this.
    /// </remarks>
    private void SetPermissions(string[] permissions) => _authentication.SetPermissions(permissions);

    private string _tenantId = "tenant-a";

    private HttpClient Client => _client ??= Factory.CreateClient();

    public ConnectionManagementTests()
    {
        _authentication.SetPermissions(PermissionNames.All);
        _store = new InMemoryIdentityProviderConnectionStore();
        _registryVersions = new InMemoryConnectionRegistryVersionStore();
        _observations = new InMemoryConnectionObservationStore();
        _registry = new TestConnectionRegistry(_store);
        _adapter = new TestAdapter();
        _settingsMigrations = new TestAdapterSettingsMigrationService();
        _roleAuthorizationService = new TestRoleAuthorizationService();
        _notifications = Substitute.For<INotificationSender>();
        _sessions = Substitute.For<IExternalAuthenticationSessionStore>();
        _managedSecretWriter = new TestManagedSecretBindingWriter();
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton(_authentication);
        services.AddElsaAuthorization();
        services.Configure<ExternalAuthenticationOptions>(options =>
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
        services.AddSingleton<IIdentityProviderConnectionStore>(_store);
        services.AddSingleton<IIdentityProviderConnectionRegistry>(_registry);
        services.AddSingleton<FinalLoginPathGuard>();
        services.AddSingleton<IConnectionRegistryVersionStore>(_registryVersions);
        services.AddSingleton<IConnectionObservationStore>(_observations);
        services.AddSingleton<ConnectionRevisionCalculator>();
        services.AddSingleton<IExternalAuthenticationAdapterRegistry>(new TestAdapterRegistry(_adapter));
        services.AddSingleton<IAdapterSettingsMigrationService>(_settingsMigrations);
        services.AddSingleton<IIdentityProviderConnectionValidityAssessor, IdentityProviderConnectionValidityAssessor>();
        services.AddSingleton<IUnlinkedIdentityPolicyRegistry>(new TestUnlinkedIdentityPolicyRegistry());
        services.AddSingleton<IExternalUserMatcherRegistry>(new TestExternalUserMatcherRegistry("allowed-matcher", "disallowed-matcher"));
        services.AddScoped(_ => Substitute.For<IPermissionGrantSourceRegistry>());
        services.AddSingleton<IPermissionDelegationAuthorizer>(Substitute.For<IPermissionDelegationAuthorizer>());
        services.AddSingleton<IRoleAuthorizationService>(_roleAuthorizationService);
        services.AddSingleton(_notifications);
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton(_sessions);
        services.AddSingleton<IManagedSecretBindingWriter>(_managedSecretWriter);
        services.AddSingleton<ISecretBindingResolver>(new TestSecretBindingResolver());
        var tenant = Substitute.For<ITenantAccessor>();
        tenant.TenantId.Returns(_ => _tenantId);
        services.AddSingleton(tenant);
        services.AddScoped<IdentityProviderConnectionManagementService>();
    }

    [Test]
    public async Task DatabaseConnectionLifecycleUsesEtagsAndPreservesItsIdentity()
    {
        var create = await Client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("contoso"));
        var created = await create.Content.ReadFromJsonAsync<ConnectionDocument>();

        await Assert.That(create.StatusCode == HttpStatusCode.Created).IsTrue().Because(await create.Content.ReadAsStringAsync());
        await Assert.That(create.Headers.ETag?.Tag).IsEqualTo("\"1\"");
        var createdDocumentValue1 = created;
        await Assert.That(createdDocumentValue1).IsOfType(typeof(ConnectionDocument));
        var createdDocument = (ConnectionDocument)createdDocumentValue1!;
        await Assert.That(createdDocument.CallbackUri).IsEqualTo("https://elsa.example/elsa/api/external-authentication/callback/contoso");
        await Assert.That(createdDocument.PreviewCallbackUri).IsEqualTo($"https://elsa.example/elsa/api/external-authentication/previews/callback/{createdDocument.Id}");

        var immutableKeyUpdate = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{createdDocument.Id}") { Content = JsonContent.Create(CreateRequest("contoso-renamed", displayName: "Updated")) };
        immutableKeyUpdate.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        var immutableKeyResponse = await Client.SendAsync(immutableKeyUpdate);
        await Assert.That(immutableKeyResponse.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        var immutableKeyContent = await immutableKeyResponse.Content.ReadAsStringAsync();
        await Assert.That(immutableKeyContent).Contains("connection_key_immutable");
        using (var errorDocument = JsonDocument.Parse(immutableKeyContent))
        {
            var correlationId = errorDocument.RootElement.GetProperty("correlationId").GetString();
            await Assert.That(correlationId).Matches("^[A-Za-z0-9_-]{1,128}$");
        }

        var update = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{createdDocument.Id}") { Content = JsonContent.Create(CreateRequest("contoso", displayName: "Updated")) };
        update.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        var updated = await Client.SendAsync(update);
        await Assert.That(updated.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(updated.Headers.ETag?.Tag).IsEqualTo("\"2\"");

        var validate = await Client.PostAsync($"/external-authentication/connections/{createdDocument.Id}/validate", null);
        await Assert.That(validate.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await validate.Content.ReadAsStringAsync()).Contains("\"valid\":true");

        var stale = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{createdDocument.Id}") { Content = JsonContent.Create(CreateRequest("contoso", displayName: "Stale")) };
        stale.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        await Assert.That((await Client.SendAsync(stale)).StatusCode).IsEqualTo(HttpStatusCode.PreconditionFailed);

        var enable = new HttpRequestMessage(HttpMethod.Post, $"/external-authentication/connections/{createdDocument.Id}/enable");
        enable.Headers.TryAddWithoutValidation("If-Match", "\"2\"");
        await Assert.That((await Client.SendAsync(enable)).StatusCode).IsEqualTo(HttpStatusCode.OK);

        var archive = new HttpRequestMessage(HttpMethod.Delete, $"/external-authentication/connections/{createdDocument.Id}");
        archive.Headers.TryAddWithoutValidation("If-Match", "\"3\"");
        await Assert.That((await Client.SendAsync(archive)).StatusCode).IsEqualTo(HttpStatusCode.OK);

        var restore = new HttpRequestMessage(HttpMethod.Post, $"/external-authentication/connections/{createdDocument.Id}/restore");
        restore.Headers.TryAddWithoutValidation("If-Match", "\"4\"");
        var restored = await Client.SendAsync(restore);
        var restoredDocument = await restored.Content.ReadFromJsonAsync<ConnectionDocument>();
        await Assert.That(restored.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var restoredConnectionValue2 = restoredDocument;
        await Assert.That(restoredConnectionValue2).IsOfType(typeof(ConnectionDocument));
        var restoredConnection = (ConnectionDocument)restoredConnectionValue2!;
        await Assert.That(restoredConnection.Id).IsEqualTo(createdDocument.Id);
        await Assert.That(restoredConnection.EnabledIntent).IsFalse();
    }

    [Test]
    public async Task ValidateRequiresCompleteConfigurationAndReturnsMissingSecretDetails()
    {
        _adapter.RequiresClientSecret = true;
        var create = await Client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("missing-secret"));
        var connectionValue3 = await create.Content.ReadFromJsonAsync<ConnectionDocument>();
        await Assert.That(connectionValue3).IsOfType(typeof(ConnectionDocument));
        var connection = (ConnectionDocument)connectionValue3!;

        var validate = await Client.PostAsync($"/external-authentication/connections/{connection.Id}/validate", null);
        var validation = JsonDocument.Parse(await validate.Content.ReadAsStringAsync()).RootElement;

        await Assert.That(validate.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(validation.GetProperty("valid").GetBoolean()).IsFalse();
        var error = (await Assert.That(validation.GetProperty("errors").EnumerateArray()).HasSingleItem())!;
        await Assert.That(error.GetProperty("field").GetString()).IsEqualTo("secretBindings.clientSecret");
        await Assert.That(error.GetProperty("code").GetString()).IsEqualTo("required");
        await Assert.That(error.GetProperty("message").GetString()).IsEqualTo("A required secret binding is missing.");
    }

    [Test]
    public async Task ConnectionResponseEmitsCanonicalUpstreamLogoutModeString()
    {
        var response = await Client.PostAsJsonAsync(
            "/external-authentication/connections",
            CreateRequest("user-choice-logout", upstreamLogoutMode: "user-choice"));
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(body.RootElement.GetProperty("upstreamLogoutMode").ValueKind).IsEqualTo(JsonValueKind.String);
        await Assert.That(body.RootElement.GetProperty("upstreamLogoutMode").GetString()).IsEqualTo("user-choice");
    }

    [Test]
    public async Task ConfigurationConnectionIsReadOnlyAndBlocksSameScopeKeyCreation()
    {
        _registry.ConfigurationConnection = ConfigurationConnection("contoso");

        var create = await Client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("contoso"));
        await Assert.That(create.StatusCode).IsEqualTo(HttpStatusCode.Conflict);

        var update = new HttpRequestMessage(HttpMethod.Put, "/external-authentication/connections/configuration-contoso") { Content = JsonContent.Create(CreateRequest("contoso")) };
        update.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        await Assert.That((await Client.SendAsync(update)).StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

        var lifecycle = new HttpRequestMessage(HttpMethod.Post, "/external-authentication/connections/configuration-contoso/disable");
        lifecycle.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        await Assert.That((await Client.SendAsync(lifecycle)).StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

        var secret = new HttpRequestMessage(HttpMethod.Put, "/external-authentication/connections/configuration-contoso/secret-bindings/clientSecret/managed") { Content = JsonContent.Create(new { resolverType = "test-managed", value = "secret" }) };
        secret.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        await Assert.That((await Client.SendAsync(secret)).StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ShadowedDatabaseConnectionAdvertisesPromotionCapabilityOnlyWhenAllowedAndActive()
    {
        const string connectionId = "database-contoso";
        _registry.ConfigurationConnection = ConfigurationConnection("contoso");
        await _store.CreateAsync(DatabaseConnection(connectionId, ConnectionScope.HostTenantId, "contoso"));

        var shadowedDatabase = await GetConnectionResponseAsync(connectionId);
        await Assert.That(shadowedDatabase.CanPromoteToConfigurationOverride).IsFalse();
        await Assert.That(shadowedDatabase.ShadowedBy?.Id).IsEqualTo("configuration-contoso");
        await Assert.That((await Assert.That((await GetConnectionResponseAsync("configuration-contoso")).Shadows).HasSingleItem())!.Id).IsEqualTo(connectionId);

        Services.GetRequiredService<IOptions<ExternalAuthenticationOptions>>().Value.AllowConfigurationConnectionOverrides = true;
        await Assert.That((await GetConnectionResponseAsync(connectionId)).CanPromoteToConfigurationOverride).IsTrue();

        var connectionValue4 = await _store.FindByIdAsync(connectionId);
        await Assert.That(connectionValue4).IsOfType(typeof(IdentityProviderConnection));
        var connection = (IdentityProviderConnection)connectionValue4!;
        connection.OverridesConfigurationConnection = true;
        await _store.UpdateAsync(connection, connection.Revision);
        await Assert.That((await GetConnectionResponseAsync(connectionId)).CanPromoteToConfigurationOverride).IsFalse();

        var connectionValue5 = await _store.FindByIdAsync(connectionId);
        await Assert.That(connectionValue5).IsOfType(typeof(IdentityProviderConnection));
        connection = (IdentityProviderConnection)connectionValue5!;
        connection.ArchivedAt = DateTimeOffset.UtcNow;
        await _store.UpdateAsync(connection, connection.Revision);
        await Assert.That((await GetConnectionResponseAsync(connectionId)).CanPromoteToConfigurationOverride).IsFalse();
    }

    [Test]
    public async Task PromotingShadowedConnectionUpdatesTheExistingRecordAndPreservesLifecycleAndSecretBindings()
    {
        const string connectionId = "database-contoso";
        _registry.ConfigurationConnection = ConfigurationConnection("contoso", isEnabled: true);
        var databaseConnection = DatabaseConnection(connectionId, ConnectionScope.HostTenantId, "contoso");
        databaseConnection.IsEnabled = true;
        databaseConnection.SecretBindings["clientSecret"] = new SecretBinding("test-managed", "preserved-secret");
        await _store.CreateAsync(databaseConnection);

        var denied = await UpdateConnectionAsync(connectionId, 1, CreateRequest("contoso", overridesConfigurationConnection: true));
        await Assert.That(denied.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var deniedConnectionResult = await _store.FindByIdAsync(connectionId);
        await Assert.That(deniedConnectionResult).IsOfType(typeof(IdentityProviderConnection));
        await Assert.That(((IdentityProviderConnection)deniedConnectionResult!).OverridesConfigurationConnection).IsFalse();

        Services.GetRequiredService<IOptions<ExternalAuthenticationOptions>>().Value.AllowConfigurationConnectionOverrides = true;
        var promoted = await UpdateConnectionAsync(connectionId, 1, CreateRequest("contoso", overridesConfigurationConnection: true));
        var promotedDocumentValue6 = await promoted.Content.ReadFromJsonAsync<ConnectionDocument>();
        await Assert.That(promotedDocumentValue6).IsOfType(typeof(ConnectionDocument));
        var promotedDocument = (ConnectionDocument)promotedDocumentValue6!;

        await Assert.That(promoted.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(promotedDocument.Id).IsEqualTo(connectionId);
        await Assert.That(promotedDocument.EnabledIntent).IsTrue();
        var persistedValue7 = await _store.FindByIdAsync(connectionId);
        await Assert.That(persistedValue7).IsOfType(typeof(IdentityProviderConnection));
        var persisted = (IdentityProviderConnection)persistedValue7!;
        await Assert.That(persisted.OverridesConfigurationConnection).IsTrue();
        await Assert.That(persisted.IsEnabled).IsTrue();
        await Assert.That(persisted.SecretBindings["clientSecret"].Reference).IsEqualTo("preserved-secret");

        var effective = await _registry.GetAsync(_tenantId);
        await Assert.That(effective.Connections.Single(x => x.Connection.Id == "configuration-contoso").IsShadowed).IsTrue();
        await Assert.That(effective.Connections.Single(x => x.Connection.Id == connectionId).IsShadowed).IsFalse();
    }

    [Test]
    public async Task PromotionOfDisabledShadowedConnectionIsBlockedWhenItWouldRemoveTheFinalLoginPath()
    {
        const string connectionId = "database-contoso";
        _registry.ConfigurationConnection = ConfigurationConnection("contoso", isEnabled: true);
        await _store.CreateAsync(DatabaseConnection(connectionId, ConnectionScope.HostTenantId, "contoso"));
        var options = Services.GetRequiredService<IOptions<ExternalAuthenticationOptions>>().Value;
        options.AllowConfigurationConnectionOverrides = true;
        options.LocalLogin.IsEnabled = false;
        options.FinalLoginPathGuard.IsEnabled = true;
        options.FinalLoginPathGuard.RequireRecoveryMethod = true;
        options.FinalLoginPathGuard.HasBreakGlassAuthentication = false;

        var promotion = await UpdateConnectionAsync(connectionId, 1, CreateRequest("contoso", overridesConfigurationConnection: true));

        await Assert.That(promotion.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That(await promotion.Content.ReadAsStringAsync()).Contains("final_login_path_guard");
        var blockedConnectionResult = await _store.FindByIdAsync(connectionId);
        await Assert.That(blockedConnectionResult).IsOfType(typeof(IdentityProviderConnection));
        await Assert.That(((IdentityProviderConnection)blockedConnectionResult!).OverridesConfigurationConnection).IsFalse();
    }

    [Test]
    public async Task ConnectionResponsesRedactDescriptorDeclaredSecretsInSettings()
    {
        var connection = DatabaseConnection("legacy-secret", ConnectionScope.HostTenantId, "legacy-secret");
        connection.AdapterSettings = JsonDocument.Parse("{\"valid\":true,\"clientSecret\":\"must-not-leave-the-server\"}").RootElement.Clone();
        await _store.CreateAsync(connection);

        var response = await Client.GetAsync("/external-authentication/connections/legacy-secret");
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).DoesNotContain("must-not-leave-the-server").WithComparison(StringComparison.Ordinal);
        await Assert.That(body).Contains("[REDACTED]").WithComparison(StringComparison.Ordinal);
    }

    [Test]
    public async Task ConnectionResponsesOmitSettingsWhenAdapterIsUnavailable()
    {
        var connection = DatabaseConnection("removed-adapter", ConnectionScope.HostTenantId, "removed-adapter");
        connection.AdapterType = "removed";
        connection.AdapterSettings = JsonDocument.Parse("{\"clientSecret\":\"must-not-leave-the-server\",\"issuer\":\"https://issuer.example\"}").RootElement.Clone();
        await _store.CreateAsync(connection);

        var response = await Client.GetAsync("/external-authentication/connections/removed-adapter");
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).DoesNotContain("must-not-leave-the-server").WithComparison(StringComparison.Ordinal);
        await Assert.That(body).DoesNotContain("issuer.example").WithComparison(StringComparison.Ordinal);
    }

    [Test]
    public async Task ConnectionsAreManagedHostWideRegardlessOfCurrentTenant()
    {
        var client = Client;
        foreach (var scope in new[] { new { kind = "default", tenantId = (string?)null }, new { kind = "tenant", tenantId = (string?)"tenant-b" } })
        {
            var response = await Client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("scope-" + scope.kind, scope));
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
            await Assert.That(await response.Content.ReadAsStringAsync()).Contains("host_scope_required");
        }

        var create = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("host-connection"));
        await Assert.That(create.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var hostValue8 = await create.Content.ReadFromJsonAsync<ConnectionDocument>();
        await Assert.That(hostValue8).IsOfType(typeof(ConnectionDocument));
        var host = (ConnectionDocument)hostValue8!;

        var update = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{host.Id}") { Content = JsonContent.Create(CreateRequest("host-connection", displayName: "Updated")) };
        update.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        await Assert.That((await client.SendAsync(update)).StatusCode).IsEqualTo(HttpStatusCode.OK);

        var secret = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{host.Id}/secret-bindings/clientSecret/managed") { Content = JsonContent.Create(new { resolverType = "test-managed", value = "secret" }) };
        secret.Headers.TryAddWithoutValidation("If-Match", "\"2\"");
        await Assert.That((await client.SendAsync(secret)).StatusCode).IsEqualTo(HttpStatusCode.OK);

        await _store.CreateAsync(DatabaseConnection("legacy-tenant", "tenant-a", "legacy-tenant"));
        await Assert.That((await client.GetAsync("/external-authentication/connections/legacy-tenant")).StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        await _store.CreateAsync(DatabaseConnection("tenant-inherited-key", "tenant-a", "tenant-inherited-key"));
        var hostCollision = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("tenant-inherited-key", new { kind = "host", tenantId = (string?)null }));
        await Assert.That(hostCollision.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task ListSupportsDeterministicPagingFiltersAndStaleObservations()
    {
        var client = Client;
        await _store.CreateAsync(DatabaseConnection("list-a", ConnectionScope.HostTenantId, "alpha", 1));
        await _store.CreateAsync(DatabaseConnection("list-b", ConnectionScope.HostTenantId, "bravo", 2));
        await _store.CreateAsync(DatabaseConnection("list-c", ConnectionScope.HostTenantId, "charlie", 3));
        await _store.CreateAsync(DatabaseConnection("legacy-tenant", "tenant-b", "not-enumerable", 4));
        await _observations.SaveLatestAsync(new ConnectionObservation("list-a", "old-material", DateTimeOffset.UtcNow, ConnectionObservationStatus.Succeeded, "connectivity", TimeSpan.Zero, "OK", [], "test"));

        var first = await client.GetFromJsonAsync<ListDocument>("/external-authentication/connections?source=database&valid=true&shadowed=false&pageSize=1");
        var firstPageValue9 = first;
        await Assert.That(firstPageValue9).IsOfType(typeof(ListDocument));
        var firstPage = (ListDocument)firstPageValue9!;
        var firstConnection = (await Assert.That(firstPage.Items).HasSingleItem())!;
        await Assert.That(firstConnection.Key).IsEqualTo("alpha");
        await Assert.That(firstConnection.LatestObservation!.IsStale).IsTrue();
        await Assert.That(firstPage.NextCursor).IsNotNull();

        var detail = await client.GetFromJsonAsync<ListConnectionDocument>("/external-authentication/connections/list-a");
        await Assert.That(detail).IsOfType(typeof(ListConnectionDocument));
        await Assert.That(((ListConnectionDocument)detail!).LatestObservation!.IsStale).IsTrue();

        var second = await client.GetFromJsonAsync<ListDocument>($"/external-authentication/connections?source=database&valid=true&shadowed=false&pageSize=1&cursor={Uri.EscapeDataString(firstPage.NextCursor!)}");
        var secondPageValue10 = second;
        await Assert.That(secondPageValue10).IsOfType(typeof(ListDocument));
        var secondPage = (ListDocument)secondPageValue10!;
        await Assert.That((await Assert.That(secondPage.Items).HasSingleItem())!.Key).IsEqualTo("bravo");
        await Assert.That((await client.GetAsync("/external-authentication/connections?source=unknown")).StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That((await client.GetAsync("/external-authentication/connections?cursor=not-a-cursor")).StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task DraftMayBeIncompleteButEnableRequiresAdapterValidationAndMigration()
    {
        var client = Client;
        var versionBefore = await _registryVersions.GetVersionAsync();
        var create = await Client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("draft", settings: new { }));
        await Assert.That(create.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var draftValue11 = await create.Content.ReadFromJsonAsync<ConnectionDocument>();
        await Assert.That(draftValue11).IsOfType(typeof(ConnectionDocument));
        var draft = (ConnectionDocument)draftValue11!;
        await Assert.That(draft.AdapterSettingsVersion).IsEqualTo(2);
        await Assert.That(await _registryVersions.IsCurrentAsync(versionBefore)).IsFalse();

        var enable = new HttpRequestMessage(HttpMethod.Post, $"/external-authentication/connections/{draft.Id}/enable");
        enable.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        await Assert.That((await client.SendAsync(enable)).StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        var future = await client.PostAsJsonAsync("/external-authentication/connections", new { key = "future", scope = new { kind = "host" }, adapterType = "test", adapterSettingsVersion = 3, adapterSettings = new { valid = true }, displayName = "Future", claimProjection = new { }, upstreamLogoutMode = "disabled" });
        await Assert.That(future.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(await future.Content.ReadAsStringAsync()).Contains("migration_unavailable");

        var secretInSettings = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("secret-in-settings", settings: new { valid = true, clientSecret = "not-allowed" }));
        await Assert.That(secretInSettings.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(await secretInSettings.Content.ReadAsStringAsync()).Contains("secret_binding_required");

        _settingsMigrations.CanMigrateVersionOne = false;
        var missing = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("missing-migration"));
        await Assert.That(missing.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(await missing.Content.ReadAsStringAsync()).Contains("migration_unavailable");

        _settingsMigrations.CanMigrateVersionOne = true;
        var uppercaseKey = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("UpperCase"));
        await Assert.That(uppercaseKey.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ExistingUnsafeSettingsRemainManageableWithoutUnsafeConfirmation()
    {
        var client = Client;
        var create = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("unsafe", settings: new { valid = true, unsafeMode = true }, confirmUnsafeSettings: true));
        await Assert.That(create.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var connectionValue12 = await create.Content.ReadFromJsonAsync<ConnectionDocument>();
        await Assert.That(connectionValue12).IsOfType(typeof(ConnectionDocument));
        var connection = (ConnectionDocument)connectionValue12!;

        _authentication.SetPermissions($"{ExternalAuthenticationResourcePermissions.Connections}:{CoreVerbs.Update}");
        var safeSettingsUpdate = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connection.Id}") { Content = JsonContent.Create(CreateRequest("unsafe", settings: new { valid = true, unsafeMode = true, label = "changed" })) };
        safeSettingsUpdate.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        await Assert.That((await client.SendAsync(safeSettingsUpdate)).StatusCode).IsEqualTo(HttpStatusCode.OK);

        _authentication.SetPermissions($"{ExternalAuthenticationResourcePermissions.Connections}:{CoreVerbs.View}");
        var validate = await client.PostAsync($"/external-authentication/connections/{connection.Id}/validate", null);
        await Assert.That(validate.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await validate.Content.ReadAsStringAsync()).Contains("\"valid\":true");

        _authentication.SetPermissions($"{ExternalAuthenticationResourcePermissions.Connections}:{CoreVerbs.Update}");
        var secret = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connection.Id}/secret-bindings/clientSecret/managed") { Content = JsonContent.Create(new { resolverType = "test-managed", value = "secret" }) };
        secret.Headers.TryAddWithoutValidation("If-Match", "\"2\"");
        await Assert.That((await client.SendAsync(secret)).StatusCode).IsEqualTo(HttpStatusCode.OK);
        await _notifications.Received().SendAsync(Arg.Is<IdentityProviderConnectionSecretBindingChanged>(x => x.FieldName == "clientSecret" && x.ResolverType == "test-managed" && !x.IsConfigured), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ManagedSecretReplacementCleansUpStagedMaterialWhenConnectionCasLoses()
    {
        var client = Client;
        var create = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("managed-secret-race"));
        var connectionValue13 = await create.Content.ReadFromJsonAsync<ConnectionDocument>();
        await Assert.That(connectionValue13).IsOfType(typeof(ConnectionDocument));
        var connection = (ConnectionDocument)connectionValue13!;
        _managedSecretWriter.BeforeReturn = async () =>
        {
            var concurrentValue14 = await _store.FindByIdAsync(connection.Id);
            await Assert.That(concurrentValue14).IsOfType(typeof(IdentityProviderConnection));
            var concurrent = (IdentityProviderConnection)concurrentValue14!;
            concurrent.DisplayName = "Concurrent update";
            await Assert.That(await _store.UpdateAsync(concurrent, concurrent.Revision)).IsOfType(typeof(ConnectionMutationResult.Updated));
        };

        var replace = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connection.Id}/secret-bindings/clientSecret/managed")
        {
            Content = JsonContent.Create(new { resolverType = "test-managed", value = "replacement" })
        };
        replace.Headers.TryAddWithoutValidation("If-Match", "\"1\"");

        await Assert.That((await client.SendAsync(replace)).StatusCode).IsEqualTo(HttpStatusCode.PreconditionFailed);
        await Assert.That(_managedSecretWriter.RemovedReferences).HasSingleItem();
        var racedConnectionResult = await _store.FindByIdAsync(connection.Id);
        await Assert.That(racedConnectionResult).IsOfType(typeof(IdentityProviderConnection));
        await Assert.That(((IdentityProviderConnection)racedConnectionResult!).SecretBindings).IsEmpty();
    }

    [Test]
    public async Task ManagedSecretReplacementCleansUpStagedMaterialWhenValidationThrows()
    {
        var create = await Client.PostAsJsonAsync(
            "/external-authentication/connections",
            CreateRequest("managed-secret-exception", unlinkedPolicy: CreateMatcherPolicy("allowed-matcher", "create-user")));
        var connectionValue16 = await create.Content.ReadFromJsonAsync<ConnectionDocument>();
        await Assert.That(connectionValue16).IsOfType(typeof(ConnectionDocument));
        var connection = (ConnectionDocument)connectionValue16!;
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

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Client.SendAsync(replace));

        await Assert.That(_managedSecretWriter.RemovedReferences).IsEquivalentTo(
            new[] { "staged-1" },
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
        var failedConnectionResult = await _store.FindByIdAsync(connection.Id);
        await Assert.That(failedConnectionResult).IsOfType(typeof(IdentityProviderConnection));
        await Assert.That(((IdentityProviderConnection)failedConnectionResult!).SecretBindings).IsEmpty();
    }

    [Test]
    public async Task DisablingWithSessionRevocationRequiresPermissionAndEmitsAggregateNotification()
    {
        var connection = DatabaseConnection("disable-with-revoke", ConnectionScope.HostTenantId, "disable-with-revoke");
        connection.IsEnabled = true;
        await _store.CreateAsync(connection);
        _sessions.RevokeActiveForConnectionAsync("disable-with-revoke", "connection_disabled", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(2);
        _authentication.SetPermissions($"{ExternalAuthenticationResourcePermissions.Connections}:{CoreVerbs.Update}");

        var forbidden = new HttpRequestMessage(HttpMethod.Post, "/external-authentication/connections/disable-with-revoke/disable?revokeActiveSessions=true");
        forbidden.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        await Assert.That((await Client.SendAsync(forbidden)).StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        await _sessions.DidNotReceive().RevokeActiveForConnectionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());

        _authentication.SetPermissions(PermissionNames.All);
        var allowed = new HttpRequestMessage(HttpMethod.Post, "/external-authentication/connections/disable-with-revoke/disable?revokeActiveSessions=true");
        allowed.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        await Assert.That((await Client.SendAsync(allowed)).StatusCode).IsEqualTo(HttpStatusCode.OK);
        await _notifications.Received().SendAsync(
            Arg.Is<ExternalAuthenticationConnectionSessionsRevoked>(x => x.SessionCount == 2 && x.Reason == "connection_disabled"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ManagedSecretReplacementRemainsPublishedWhenPostCommitNotificationFails()
    {
        var client = Client;
        var create = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("managed-secret-notification"));
        var connectionValue17 = await create.Content.ReadFromJsonAsync<ConnectionDocument>();
        await Assert.That(connectionValue17).IsOfType(typeof(ConnectionDocument));
        var connection = (ConnectionDocument)connectionValue17!;
        _notifications
            .SendAsync(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException("Notification failure")));

        var replace = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connection.Id}/secret-bindings/clientSecret/managed")
        {
            Content = JsonContent.Create(new { resolverType = "test-managed", value = "replacement" })
        };
        replace.Headers.TryAddWithoutValidation("If-Match", "\"1\"");

        await Assert.That((await client.SendAsync(replace)).StatusCode).IsEqualTo(HttpStatusCode.OK);
        var persistedValue18 = await _store.FindByIdAsync(connection.Id);
        await Assert.That(persistedValue18).IsOfType(typeof(IdentityProviderConnection));
        var persisted = (IdentityProviderConnection)persistedValue18!;
        await Assert.That(persisted.SecretBindings["clientSecret"].Reference).IsEqualTo("staged-1");
        await Assert.That(_managedSecretWriter.RemovedReferences).IsEmpty();
    }

    [Test]
    public async Task ManagedSecretWriterMustStageAReferenceDistinctFromTheLiveBinding()
    {
        var client = Client;
        var create = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("managed-secret-distinct"));
        var connectionValue19 = await create.Content.ReadFromJsonAsync<ConnectionDocument>();
        await Assert.That(connectionValue19).IsOfType(typeof(ConnectionDocument));
        var connection = (ConnectionDocument)connectionValue19!;
        var first = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connection.Id}/secret-bindings/clientSecret/managed")
        {
            Content = JsonContent.Create(new { resolverType = "test-managed", value = "first" })
        };
        first.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        await Assert.That((await client.SendAsync(first)).StatusCode).IsEqualTo(HttpStatusCode.OK);

        _managedSecretWriter.ReferenceToReturn = "staged-1";
        var invalid = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connection.Id}/secret-bindings/clientSecret/managed")
        {
            Content = JsonContent.Create(new { resolverType = "test-managed", value = "second" })
        };
        invalid.Headers.TryAddWithoutValidation("If-Match", "\"2\"");

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => client.SendAsync(invalid));
        var retainedConnectionResult = await _store.FindByIdAsync(connection.Id);
        await Assert.That(retainedConnectionResult).IsOfType(typeof(IdentityProviderConnection));
        await Assert.That(((IdentityProviderConnection)retainedConnectionResult!).SecretBindings["clientSecret"].Reference).IsEqualTo("staged-1");
        await Assert.That(_managedSecretWriter.RemovedReferences).IsEmpty();
    }

    [Test]
    public async Task GeneralConnectionPayloadCannotInjectOrClearSecretBindings()
    {
        var client = Client;
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
        await Assert.That(injectedCreate.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(await injectedCreate.Content.ReadAsStringAsync()).Contains("secret_bindings_mutation_not_allowed");

        var create = await client.PostAsJsonAsync("/external-authentication/connections", CreateRequest("cannot-clear-secret"));
        var connectionValue20 = await create.Content.ReadFromJsonAsync<ConnectionDocument>();
        await Assert.That(connectionValue20).IsOfType(typeof(ConnectionDocument));
        var connection = (ConnectionDocument)connectionValue20!;
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
        await Assert.That((await client.SendAsync(clear)).StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task MatcherPolicyRejectsAMatcherDisallowedByDeployment()
    {
        var response = await Client.PostAsJsonAsync(
            "/external-authentication/connections",
            CreateRequest("disallowed-matcher", unlinkedPolicy: CreateMatcherPolicy("disallowed-matcher", "reject")));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(await response.Content.ReadAsStringAsync()).Contains("validation_failed");
    }

    [Test]
    public async Task MatcherCreateUserFallbackRequiresRoleDelegation()
    {
        _roleAuthorizationService.CanAssignRoles = false;

        var response = await Client.PostAsJsonAsync(
            "/external-authentication/connections",
            CreateRequest("matcher-roles", unlinkedPolicy: CreateMatcherPolicy("allowed-matcher", "create-user")));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(await response.Content.ReadAsStringAsync()).Contains("validation_failed");
        await Assert.That(_roleAuthorizationService.LastRequestedRoleIds).IsEquivalentTo(
            new[] { "workflow-user" },
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task SettingDefaultRolesRequiresThePolicyDefaultRolesPermission()
    {
        // The actor may create connections and manage policies, but not decide what auto-created users get.
        // Before #7977 that was inexpressible: policies:update guarded the policy while the roles inside it
        // were guarded only by the subset rule, so any connection administrator could set them.
        SetPermissions(
        [
            $"{ExternalAuthenticationResourcePermissions.Connections}:{CoreVerbs.Create}",
            $"{ExternalAuthenticationResourcePermissions.Policies}:{CoreVerbs.Update}"
        ]);

        var response = await Client.PostAsJsonAsync(
            "/external-authentication/connections",
            CreateRequest("roles-guard", unlinkedPolicy: CreateMatcherPolicy("allowed-matcher", "create-user")));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(await response.Content.ReadAsStringAsync()).Contains("policy default roles update permission");
    }

    [Test]
    public async Task HoldingThePolicyDefaultRolesPermissionClearsThatObjection()
    {
        SetPermissions(
        [
            $"{ExternalAuthenticationResourcePermissions.Connections}:{CoreVerbs.Create}",
            $"{ExternalAuthenticationResourcePermissions.Policies}:{CoreVerbs.Update}",
            $"{ExternalAuthenticationResourcePermissions.PolicyDefaultRoles}:{CoreVerbs.Update}"
        ]);

        var response = await Client.PostAsJsonAsync(
            "/external-authentication/connections",
            CreateRequest("roles-allowed", unlinkedPolicy: CreateMatcherPolicy("allowed-matcher", "create-user")));

        // The subset rule is a separate question and still applies; only this objection must be gone.
        await Assert.That(await response.Content.ReadAsStringAsync()).DoesNotContain("policy default roles update permission");
    }

    [Test]
    public async Task LeavingStoredDefaultRolesAloneNeedsNoPermission()
    {
        // Validation runs on every update, on enabling a connection, and on read-only validate. Keying the
        // permission off the roles being present rather than changing meant that once anyone set default
        // roles, an administrator without it could no longer edit an unrelated field on that connection.
        var created = await Client.PostAsJsonAsync(
            "/external-authentication/connections",
            CreateRequest("roles-untouched", unlinkedPolicy: CreateMatcherPolicy("allowed-matcher", "create-user")));
        await Assert.That(created.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var id = (await created.Content.ReadFromJsonAsync<ConnectionDocument>())!.Id;
        var revision = created.Headers.ETag!.Tag;

        // Now act as someone who may edit connections and policies, but not decide default roles.
        SetPermissions(
        [
            $"{ExternalAuthenticationResourcePermissions.Connections}:{CoreVerbs.Update}",
            $"{ExternalAuthenticationResourcePermissions.Policies}:{CoreVerbs.Update}"
        ]);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{id}")
        {
            Content = JsonContent.Create(CreateRequest("roles-untouched", displayName: "Renamed", unlinkedPolicy: CreateMatcherPolicy("allowed-matcher", "create-user")))
        };
        request.Headers.TryAddWithoutValidation("If-Match", revision);

        var response = await Client.SendAsync(request);

        // Asserting the status, not just the absence of a message: DoesNotContain alone passes for any
        // failure response, which would make this test vacuous exactly when it matters.
        await Assert.That(response.IsSuccessStatusCode).IsTrue().Because($"expected success, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    [Test]
    public async Task AbandoningACreateUserPolicyStillCountsAsChangingDefaultRoles()
    {
        // Turning off a stored create-user fallback removes its automatic role assignments. That is a
        // decision about what auto-created users receive, so it needs the same permission as editing the
        // list -- checking only create-user candidates would have let it through unguarded. Expressed here by
        // changing noMatchAction rather than the policy type, because the test registry only knows match-user.
        var created = await Client.PostAsJsonAsync(
            "/external-authentication/connections",
            CreateRequest("roles-abandoned", unlinkedPolicy: CreateMatcherPolicy("allowed-matcher", "create-user")));
        await Assert.That(created.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var id = (await created.Content.ReadFromJsonAsync<ConnectionDocument>())!.Id;
        var revision = created.Headers.ETag!.Tag;

        SetPermissions(
        [
            $"{ExternalAuthenticationResourcePermissions.Connections}:{CoreVerbs.Update}",
            $"{ExternalAuthenticationResourcePermissions.Policies}:{CoreVerbs.Update}"
        ]);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{id}")
        {
            Content = JsonContent.Create(CreateRequest("roles-abandoned", unlinkedPolicy: CreateMatcherPolicy("allowed-matcher", "reject")))
        };
        request.Headers.TryAddWithoutValidation("If-Match", revision);

        var response = await Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(await response.Content.ReadAsStringAsync()).Contains("policy default roles update permission");
    }

    [Test]
    public async Task OmittingAStoredCreateUserPolicyStillCountsAsChangingDefaultRoles()
    {
        // The abandonment guard above works by switching noMatchAction, but a PUT can drop the stored
        // fallback more quietly: omit unlinkedPolicy altogether. Normalization does not carry the stored
        // policy forward, so a null candidate clears it -- and its role assignments with it. That is the
        // same decision as switching to 'reject', so it needs the same permission.
        var (id, revision) = await CreateConnectionAsync(
            CreateRequest("roles-omitted", unlinkedPolicy: CreateMatcherPolicy("allowed-matcher", "create-user")));

        SetPermissions(UpdateWithoutDefaultRolesPermission);

        var response = await PutConnectionAsync(id, revision, CreateRequest("roles-omitted"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(await response.Content.ReadAsStringAsync()).Contains("policy default roles update permission");
    }

    [Test]
    public async Task IntroducingACreateUserPolicyOnAPolicylessConnectionRequiresThePermission()
    {
        // The reverse transition: the stored connection has no policy, so the baseline role set is empty,
        // and an update that introduces a create-user fallback with roles is deciding what auto-created
        // users receive.
        var (id, revision) = await CreateConnectionAsync(CreateRequest("roles-introduced"));

        SetPermissions(UpdateWithoutDefaultRolesPermission);

        var response = await PutConnectionAsync(id, revision,
            CreateRequest("roles-introduced", unlinkedPolicy: CreateMatcherPolicy("allowed-matcher", "create-user")));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(await response.Content.ReadAsStringAsync()).Contains("policy default roles update permission");
    }

    [Test]
    public async Task ClearingAPolicyThatAssignsNoRolesNeedsNoPermission()
    {
        // Clearing a create-user fallback whose role list is already empty changes nothing about what
        // auto-created users receive, so the guard must stay quiet -- it keys off the effective set
        // changing, not off the policy disappearing.
        var (id, revision) = await CreateConnectionAsync(
            CreateRequest("no-roles-cleared", unlinkedPolicy: CreateMatcherPolicyWithoutDefaultRoles("allowed-matcher", "create-user")));

        SetPermissions(UpdateWithoutDefaultRolesPermission);

        var response = await PutConnectionAsync(id, revision, CreateRequest("no-roles-cleared"));

        await Assert.That(response.IsSuccessStatusCode).IsTrue().Because($"expected success, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    /// <summary>May edit connections and policies, but not decide default roles -- the #7977 separation.</summary>
    private static readonly string[] UpdateWithoutDefaultRolesPermission =
    [
        $"{ExternalAuthenticationResourcePermissions.Connections}:{CoreVerbs.Update}",
        $"{ExternalAuthenticationResourcePermissions.Policies}:{CoreVerbs.Update}"
    ];

    private async Task<(string Id, string Revision)> CreateConnectionAsync(object request)
    {
        var created = await Client.PostAsJsonAsync("/external-authentication/connections", request);
        await Assert.That(created.StatusCode).IsEqualTo(HttpStatusCode.Created);
        return ((await created.Content.ReadFromJsonAsync<ConnectionDocument>())!.Id, created.Headers.ETag!.Tag);
    }

    private async Task<HttpResponseMessage> PutConnectionAsync(string id, string revision, object request)
    {
        using var message = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{id}")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.TryAddWithoutValidation("If-Match", revision);
        return await Client.SendAsync(message);
    }

    [Test]
    public async Task ValidatingAConfigurationOwnedConnectionDoesNotReadItsRolesAsNew()
    {
        // A configuration-owned connection has no database row, so taking the baseline from the database
        // store alone made its configured roles look newly assigned every time. Validation only needs
        // connections:view, so a caller with exactly that could not validate one at all.
        var configuration = ConfigurationConnection("config-roles", isEnabled: true);
        configuration.UnlinkedPolicy = CreateMatcherPolicy("allowed-matcher", "create-user");
        _registry.ConfigurationConnection = configuration;

        SetPermissions([$"{ExternalAuthenticationResourcePermissions.Connections}:{CoreVerbs.View}"]);

        var response = await Client.PostAsync($"/external-authentication/connections/{configuration.Id}/validate", null);

        await Assert.That(await response.Content.ReadAsStringAsync()).DoesNotContain("policy default roles update permission");
    }

    [Test]
    public async Task APolicyThatSetsNoDefaultRolesNeedsNoExtraPermission()
    {
        // Creating with none decides nothing, so it needs nothing. Changing a stored set -- including
        // clearing it -- is deciding, and is covered by the permission.
        SetPermissions(
        [
            $"{ExternalAuthenticationResourcePermissions.Connections}:{CoreVerbs.Create}",
            $"{ExternalAuthenticationResourcePermissions.Policies}:{CoreVerbs.Update}"
        ]);

        var response = await Client.PostAsJsonAsync(
            "/external-authentication/connections",
            CreateRequest("roles-empty", unlinkedPolicy: CreateMatcherPolicyWithoutDefaultRoles("allowed-matcher", "create-user")));

        await Assert.That(await response.Content.ReadAsStringAsync()).DoesNotContain("policy default roles update permission");
    }

    private static PolicySelection CreateMatcherPolicyWithoutDefaultRoles(string matcherType, string noMatchAction) => new(
        "match-user",
        1,
        JsonSerializer.SerializeToElement(new
        {
            matcher = new { type = matcherType, settingsVersion = 1, settings = new { } },
            noMatchAction,
            defaultRoleIds = Array.Empty<string>()
        }));

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
        var response = await Client.GetAsync($"/external-authentication/connections/{connectionId}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var resultValue21 = await response.Content.ReadFromJsonAsync<ConnectionDocument>();
        await Assert.That(resultValue21).IsOfType(typeof(ConnectionDocument));
        return (ConnectionDocument)resultValue21!;
    }

    private async Task<HttpResponseMessage> UpdateConnectionAsync(string connectionId, long revision, object request)
    {
        var update = new HttpRequestMessage(HttpMethod.Put, $"/external-authentication/connections/{connectionId}") { Content = JsonContent.Create(request) };
        update.Headers.TryAddWithoutValidation("If-Match", $"\"{revision}\"");
        return await Client.SendAsync(update);
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
