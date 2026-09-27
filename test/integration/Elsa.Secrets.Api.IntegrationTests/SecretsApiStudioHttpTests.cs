using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Elsa;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore.Sqlite.Extensions;
using Elsa.Testing.Shared;
using Elsa.Tenants;
using Elsa.Tenants.AspNetCore;
using Elsa.Tenants.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FastEndpoints;
using Refit;
using Elsa.Studio.Secrets.Client;
using Elsa.Studio.Secrets.Models;

namespace Elsa.Secrets.Api.IntegrationTests;

[Collection(nameof(SecretsApiHttpCollection))]
public sealed class SecretsApiStudioHttpTests : IAsyncLifetime
{
    private static readonly byte[] EncryptionKey = "0123456789abcdef0123456789abcdef"u8.ToArray();
    private static readonly (string Verb, string Route)[] ExpectedRoutes =
    [
        ("DELETE", "/secrets/{name}"),
        ("GET", "/secrets"),
        ("GET", "/secrets/descriptors"),
        ("GET", "/secrets/{name}"),
        ("POST", "/secrets"),
        ("POST", "/secrets/picker"),
        ("POST", "/secrets/{name}"),
        ("POST", "/secrets/{name}/revoke"),
        ("POST", "/secrets/{name}/rotate"),
        ("POST", "/secrets/{name}/test")
    ];
    private const string AllSecretsPermissions = "secrets:view,secrets:write,secrets:delete,secrets:test";
    private const string DefaultIdentity = "contract-test-user";
    private const string MatrixSecretName = "authorization-matrix";
    private const string MatrixCreatedSecretName = "authorization-matrix-created";
    private const string MatrixSeedDisplayName = "Matrix seed";
    private const string MatrixUpdatedDisplayName = "Changed by matrix principal";
    private static readonly IHttpContentSerializer StudioSerializer = new RefitSettings().ContentSerializer;
    private static readonly string[] ReadOperations = ["list", "descriptors", "get", "picker"];
    private static readonly string[] WriteOperations = ["update", "rotate", "revoke", "create"];
    private static readonly string[] AllOperations = [.. ReadOperations, "test", .. WriteOperations, "delete"];

    // Delete is sent separately, last, so the effect of every earlier call is still observable.
    private static readonly SecretOperation[] MatrixOperations =
    [
        new("list", HttpMethod.Get, "/secrets"),
        new("descriptors", HttpMethod.Get, "/secrets/descriptors"),
        new("get", HttpMethod.Get, $"/secrets/{MatrixSecretName}"),
        new("picker", HttpMethod.Post, "/secrets/picker", new SecretPickerRequest()),
        new("test", HttpMethod.Post, $"/secrets/{MatrixSecretName}/test"),
        new("update", HttpMethod.Post, $"/secrets/{MatrixSecretName}", new UpdateSecretRequest { DisplayName = MatrixUpdatedDisplayName }),
        new("rotate", HttpMethod.Post, $"/secrets/{MatrixSecretName}/rotate", new RotateSecretRequest { Value = "matrix-rotated-value" }),
        new("revoke", HttpMethod.Post, $"/secrets/{MatrixSecretName}/revoke"),
        new("create", HttpMethod.Post, "/secrets", new CreateSecretRequest { Name = MatrixCreatedSecretName, Value = "matrix-created-value" })
    ];

    /// <summary>
    /// Permission claim values and the canonical operations each may perform. The legacy rows are the exact tokens the
    /// pinned Extensions 154ba15 endpoints declared; none is treated as an alias. Only <c>secrets:write</c> and
    /// <c>secrets:delete</c> share a spelling with a Core token, so they take the Core meaning.
    /// </summary>
    public static TheoryData<string?, string[]> PermissionMatrix => new()
    {
        { null, [] },
        { "unrelated:view", [] },
        { "secrets:view", ReadOperations },
        { "secrets:*", AllOperations },
        { "*", AllOperations },
        { "read:secrets", [] },
        { "secrets:read", [] },
        { "write:secrets", [] },
        { "secrets:write", WriteOperations },
        { "secrets:delete", ["delete"] }
    };

    public static TheoryData<string, string?> CrossTenantPrincipals => new()
    {
        { "secrets:view", "tenant-c" },
        { "secrets:view", null },
        { "secrets:*", "tenant-c" },
        { "secrets:*", null }
    };

    private WebApplication? _app;
    private string? _databasePath;
    private bool _previousSecuritySetting;

    public async Task InitializeAsync()
    {
        _previousSecuritySetting = EndpointSecurityOptions.SecurityIsEnabled;
        await StartAppAsync(securityEnabled: true);
    }

    private async Task StartAppAsync(bool securityEnabled)
    {
        EndpointSecurityOptions.SecurityIsEnabled = securityEnabled;
        _databasePath = Path.Combine(Path.GetTempPath(), $"elsa-secrets-api-contract-{Guid.NewGuid():N}.sqlite");

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAuthentication(TestAuthenticationHandler.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationScheme, _ => { });
        builder.Services.AddAuthorization();

        var tenantAccessor = new DefaultTenantAccessor();
        builder.Services.AddSingleton<ITenantAccessor>(tenantAccessor);
        builder.Services.AddElsa(elsa => elsa
            .UseSecrets(secrets =>
            {
                secrets.ConfigureOptions = options => options.EncryptionKey = EncryptionKey;
                secrets.UseEntityFrameworkCore(ef =>
                {
                    ef.UseContextPooling = false;
                    ef.UseSqlite($"Data Source={_databasePath};Default Timeout=30");
                });
            })
            .UseSecretsJavaScript()
            .UseTenants(tenants =>
            {
                tenants.UseConfigurationBasedTenantsProvider(options => options.Tenants =
                [
                    new Tenant { Id = Tenant.DefaultTenantId, Name = "Default" },
                    new Tenant { Id = "tenant-a", Name = "Tenant A" },
                    new Tenant { Id = "tenant-b", Name = "Tenant B" },
                    new Tenant { Id = "tenant-c", Name = "Tenant C" }
                ]);
                tenants.ConfigureMultitenancy(options =>
                    options.TenantResolverPipelineBuilder = new TenantResolverPipelineBuilder()
                        .Append<DefaultTenantResolver>()
                        .Append<HeaderTenantResolver>());
            })
            .UseTenantHttpRouting(feature => feature.WithTenantHeader("X-Tenant-Id")));

        _app = builder.Build();
        _app.UseTenants();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseFastEndpoints();

        await _app.StartAsync();
        await _app.Services.PopulateRegistriesAsync();
    }

    public async Task DisposeAsync()
    {
        EndpointSecurityOptions.SecurityIsEnabled = _previousSecuritySetting;
        await DisposeAppAsync();
    }

    private async Task DisposeAppAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
            _app = null;
        }

        if (_databasePath is not null)
        {
            foreach (var path in new[] { _databasePath, $"{_databasePath}-shm", $"{_databasePath}-wal" })
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            _databasePath = null;
        }
    }

    [Fact]
    public async Task PinnedStudioRefitClientCompletesSecretOperationsWithoutReturningStoredValue()
    {
        using var client = CreateClient(AllSecretsPermissions, "tenant-a", out var capture);
        var api = RestService.For<ISecretsApi>(client);
        const string name = "studio-http-contract";
        const string originalValue = "initial-secret-never-echo";

        var created = await api.CreateAsync(new CreateSecretRequest
        {
            Name = name,
            DisplayName = "Studio HTTP contract",
            Description = "Created through the pinned Refit client",
            Scope = "workflow",
            Value = originalValue
        });

        Assert.Equal(name, created.Name);
        Assert.Equal("Studio HTTP contract", created.DisplayName);
        Assert.Equal(1, created.CurrentVersion);

        var listed = await api.ListAsync(search: "studio-http-contract", typeName: SecretTypeNames.Text,
            storeName: SecretStoreNames.Encrypted, scope: "workflow", status: SecretStatus.Active, page: 0, pageSize: 10);
        Assert.Contains(listed.Items, item => item.Id == created.Id);

        var loaded = await api.GetAsync(name);
        Assert.Equal(created.Id, loaded.Id);

        var updated = await api.UpdateAsync(name, new UpdateSecretRequest
        {
            DisplayName = "Renamed by Studio",
            Description = "Updated over HTTP"
        });
        Assert.Equal("Renamed by Studio", updated.DisplayName);

        var descriptors = await api.GetDescriptorsAsync();
        Assert.Contains(descriptors.Types, descriptor => descriptor.Name == SecretTypeNames.Text);
        Assert.Contains(descriptors.Stores, descriptor => descriptor.Name == SecretStoreNames.Encrypted);
        Assert.Equal(descriptors.Types.Count, descriptors.Types.Select(descriptor => descriptor.Name).Distinct().Count());
        Assert.Equal(descriptors.Stores.Count, descriptors.Stores.Select(descriptor => descriptor.Name).Distinct().Count());

        var picked = await api.PickAsync(new SecretPickerRequest
        {
            TypeNames = [SecretTypeNames.Text],
            StoreNames = [SecretStoreNames.Encrypted],
            Scope = "workflow",
            ActiveOnly = true
        });
        Assert.Contains(picked.Items, item => item.Id == created.Id);

        var rotated = await api.RotateAsync(name, new RotateSecretRequest { Value = "rotated-secret-never-echo" });
        Assert.Equal(2, rotated.CurrentVersion);
        Assert.True((await api.TestAsync(name)).Succeeded);

        var revoked = await api.RevokeAsync(name);
        Assert.Equal(SecretStatus.Revoked, revoked.Status);
        await api.DeleteAsync(name);

        AssertNoSecretMaterial(capture.ResponseBodies, originalValue, "rotated-secret-never-echo");
    }

    [Fact]
    public void CoreSecretsFeatureRegistersExactlyOneEndpointForEachCanonicalRoute()
    {
        var actual = ((IEndpointRouteBuilder)_app!).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/secrets", StringComparison.Ordinal) == true)
            .SelectMany(endpoint => endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods
                .Select(verb => (Verb: verb, Route: endpoint.RoutePattern.RawText!)))
            .ToArray();

        Assert.Equal(ExpectedRoutes.Length, actual.Length);
        Assert.Equal(actual.Length, actual.Distinct().Count());
        Assert.Equal(ExpectedRoutes.OrderBy(route => route.Verb).ThenBy(route => route.Route),
            actual.OrderBy(route => route.Verb).ThenBy(route => route.Route));
    }

    [Fact]
    public async Task PermissionGatesRunThroughHttpAndKeepLegacyReadAndWriteFromImplyingCoreViewOrDelete()
    {
        using var anonymous = CreateClient(null, null, out var anonymousCapture);
        using var anonymousResponse = await anonymous.GetAsync("/secrets");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var legacyRead = CreateClient("secrets:read", null, out var legacyReadCapture);
        using var deniedRead = await legacyRead.GetAsync("/secrets");
        Assert.Equal(HttpStatusCode.Forbidden, deniedRead.StatusCode);

        using var legacyListRead = CreateClient("read:secrets", null, out var legacyListReadCapture);
        using var deniedLegacyList = await legacyListRead.GetAsync("/secrets");
        Assert.Equal(HttpStatusCode.Forbidden, deniedLegacyList.StatusCode);

        using var legacyWriter = CreateClient("write:secrets", null, out var legacyWriterCapture);
        var legacyWriteDenied = await Assert.ThrowsAsync<ApiException>(() => RestService.For<ISecretsApi>(legacyWriter)
            .CreateAsync(new CreateSecretRequest { Name = "legacy-write-denied", Value = "legacy-permission-secret" }));
        Assert.Equal(HttpStatusCode.Forbidden, legacyWriteDenied.StatusCode);

        using var writer = CreateClient("secrets:write", null, out var writerCapture);
        var api = RestService.For<ISecretsApi>(writer);
        const string name = "http-permission-contract";
        var created = await api.CreateAsync(new CreateSecretRequest { Name = name, Value = "permission-secret" });
        Assert.Equal(name, created.Name);

        var deniedList = await Assert.ThrowsAsync<ApiException>(() => api.ListAsync());
        Assert.Equal(HttpStatusCode.Forbidden, deniedList.StatusCode);

        using var deniedDelete = await writer.DeleteAsync($"/secrets/{name}");
        Assert.Equal(HttpStatusCode.Forbidden, deniedDelete.StatusCode);

        using var reader = CreateClient("secrets:view", null, out var readerCapture);
        var viewOnlyApi = RestService.For<ISecretsApi>(reader);
        Assert.Contains((await viewOnlyApi.ListAsync()).Items, item => item.Id == created.Id);
        using var stillPresent = await reader.GetAsync($"/secrets/{name}");
        Assert.Equal(HttpStatusCode.OK, stillPresent.StatusCode);
        using var legacyInput = await reader.GetAsync($"/secrets/{name}/input");
        Assert.Equal(HttpStatusCode.NotFound, legacyInput.StatusCode);

        var testDenied = await Assert.ThrowsAsync<ApiException>(() => viewOnlyApi.TestAsync(name));
        Assert.Equal(HttpStatusCode.Forbidden, testDenied.StatusCode);

        var updateDenied = await Assert.ThrowsAsync<ApiException>(() => viewOnlyApi.UpdateAsync(name, new UpdateSecretRequest
        {
            DisplayName = "Must not update",
            Description = "View permission does not grant write"
        }));
        Assert.Equal(HttpStatusCode.Forbidden, updateDenied.StatusCode);

        var rotateDenied = await Assert.ThrowsAsync<ApiException>(() => viewOnlyApi.RotateAsync(name, new RotateSecretRequest
        {
            Value = "must-not-rotate"
        }));
        Assert.Equal(HttpStatusCode.Forbidden, rotateDenied.StatusCode);

        var revokeDenied = await Assert.ThrowsAsync<ApiException>(() => viewOnlyApi.RevokeAsync(name));
        Assert.Equal(HttpStatusCode.Forbidden, revokeDenied.StatusCode);

        var writerUpdate = await api.UpdateAsync(name, new UpdateSecretRequest
        {
            DisplayName = "Updated with write permission",
            Description = "Write permission permits update"
        });
        Assert.Equal("Updated with write permission", writerUpdate.DisplayName);
        Assert.Equal(2, (await api.RotateAsync(name, new RotateSecretRequest { Value = "rotated-with-write" })).CurrentVersion);

        using var testPermission = CreateClient("secrets:test", null, out var testCapture);
        var testAllowed = await RestService.For<ISecretsApi>(testPermission).TestAsync(name);
        Assert.True(testAllowed.Succeeded);

        Assert.Equal(SecretStatus.Revoked, (await api.RevokeAsync(name)).Status);

        using var deleter = CreateClient("secrets:delete", null, out var deleterCapture);
        using var deleted = await deleter.DeleteAsync($"/secrets/{name}");
        Assert.True(deleted.IsSuccessStatusCode);

        AssertNoSecretMaterial(
            anonymousCapture.ResponseBodies
                .Concat(legacyReadCapture.ResponseBodies)
                .Concat(legacyListReadCapture.ResponseBodies)
                .Concat(legacyWriterCapture.ResponseBodies)
                .Concat(writerCapture.ResponseBodies)
                .Concat(readerCapture.ResponseBodies)
                .Concat(testCapture.ResponseBodies)
                .Concat(deleterCapture.ResponseBodies),
            "permission-secret", "legacy-permission-secret", "must-not-rotate", "rotated-with-write");

        using var missingAfterDelete = await reader.GetAsync($"/secrets/{name}");
        Assert.Equal(HttpStatusCode.NotFound, missingAfterDelete.StatusCode);
    }

    [Fact]
    public async Task PickerInlineCreateCapabilityRequiresWritePermission()
    {
        var request = new SecretPickerRequest();

        using var roleless = CreateClient("unrelated:view", "tenant-a", out _);
        using var rolelessResponse = await roleless.PostAsJsonAsync("/secrets/picker", request);
        Assert.Equal(HttpStatusCode.Forbidden, rolelessResponse.StatusCode);

        using var viewOnly = CreateClient("secrets:view", "tenant-a", out _);
        var viewOnlyResponse = await RestService.For<ISecretsApi>(viewOnly).PickAsync(request);
        Assert.False(viewOnlyResponse.CanCreateInline);

        using var writeOnly = CreateClient("secrets:write", "tenant-a", out _);
        using var writeOnlyResponse = await writeOnly.PostAsJsonAsync("/secrets/picker", request);
        Assert.Equal(HttpStatusCode.Forbidden, writeOnlyResponse.StatusCode);

        using var writer = CreateClient("secrets:view,secrets:write", "tenant-a", out _);
        var writerResponse = await RestService.For<ISecretsApi>(writer).PickAsync(request);
        Assert.True(writerResponse.CanCreateInline);
    }

    [Fact]
    public async Task PickerAllowsAnonymousInlineCreateWhenEndpointSecurityIsDisabled()
    {
        await DisposeAppAsync();
        await StartAppAsync(securityEnabled: false);

        using var anonymous = CreateClient(null, "tenant-a", out _);
        using var response = await anonymous.PostAsJsonAsync("/secrets/picker", new SecretPickerRequest());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var picker = await response.Content.ReadFromJsonAsync<SecretPickerResponse>();
        Assert.NotNull(picker);
        Assert.True(picker.CanCreateInline);
    }

    [Fact]
    public async Task HttpTenantResolutionSeparatesSameNameSecretsAcrossTenants()
    {
        using var tenantAClient = CreateClient(AllSecretsPermissions, "tenant-a", out var tenantACapture);
        using var tenantBClient = CreateClient("secrets:view,secrets:write,secrets:test", "tenant-b", out var tenantBCapture);
        using var tenantCClient = CreateClient(AllSecretsPermissions, "tenant-c", out var tenantCCapture);
        using var defaultTenantClient = CreateClient(AllSecretsPermissions, null, out var defaultTenantCapture);
        var tenantA = RestService.For<ISecretsApi>(tenantAClient);
        var tenantB = RestService.For<ISecretsApi>(tenantBClient);
        var tenantC = RestService.For<ISecretsApi>(tenantCClient);
        var defaultTenant = RestService.For<ISecretsApi>(defaultTenantClient);
        const string sharedName = "tenant-scoped-contract";

        var secretA = await tenantA.CreateAsync(new CreateSecretRequest { Name = sharedName, Scope = "workflow", Value = "tenant-a-secret" });
        var secretB = await tenantB.CreateAsync(new CreateSecretRequest { Name = sharedName, Scope = "workflow", Value = "tenant-b-secret" });

        Assert.NotEqual(secretA.Id, secretB.Id);
        Assert.Equal(secretA.Id, (await tenantA.GetAsync(sharedName)).Id);
        Assert.Equal(secretB.Id, (await tenantB.GetAsync(sharedName)).Id);
        Assert.Contains((await tenantA.ListAsync()).Items, item => item.Id == secretA.Id);
        Assert.DoesNotContain((await tenantA.ListAsync()).Items, item => item.Id == secretB.Id);
        Assert.Contains((await tenantB.ListAsync()).Items, item => item.Id == secretB.Id);
        Assert.DoesNotContain((await tenantB.ListAsync()).Items, item => item.Id == secretA.Id);

        var pickerRequest = new SecretPickerRequest
        {
            TypeNames = [SecretTypeNames.Text],
            StoreNames = [SecretStoreNames.Encrypted],
            Scope = "workflow"
        };
        Assert.Collection((await tenantA.PickAsync(pickerRequest)).Items, item => Assert.Equal(secretA.Id, item.Id));
        Assert.Collection((await tenantB.PickAsync(pickerRequest)).Items, item => Assert.Equal(secretB.Id, item.Id));
        var tenantCPicker = await tenantC.PickAsync(pickerRequest);
        Assert.Empty(tenantCPicker.Items);
        Assert.Empty((await defaultTenant.PickAsync(pickerRequest)).Items);

        Assert.True((await tenantA.TestAsync(sharedName)).Succeeded);
        Assert.True((await tenantB.TestAsync(sharedName)).Succeeded);
        var tenantCTest = await tenantC.TestAsync(sharedName);
        Assert.False(tenantCTest.Succeeded);
        Assert.Contains("not found", tenantCTest.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False((await defaultTenant.TestAsync(sharedName)).Succeeded);

        foreach (var isolatedTenant in new[] { tenantC, defaultTenant })
        {
            var missing = await Assert.ThrowsAsync<ApiException>(() => isolatedTenant.GetAsync(sharedName));
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            var missingUpdate = await Assert.ThrowsAsync<ApiException>(() => isolatedTenant.UpdateAsync(sharedName, new UpdateSecretRequest { DisplayName = "Must not cross tenants" }));
            Assert.Equal(HttpStatusCode.NotFound, missingUpdate.StatusCode);
            var missingRotate = await Assert.ThrowsAsync<ApiException>(() => isolatedTenant.RotateAsync(sharedName, new RotateSecretRequest { Value = "must-not-cross-tenants" }));
            Assert.Equal(HttpStatusCode.NotFound, missingRotate.StatusCode);
            var missingRevoke = await Assert.ThrowsAsync<ApiException>(() => isolatedTenant.RevokeAsync(sharedName));
            Assert.Equal(HttpStatusCode.NotFound, missingRevoke.StatusCode);
            var missingDelete = await Assert.ThrowsAsync<ApiException>(() => isolatedTenant.DeleteAsync(sharedName));
            Assert.Equal(HttpStatusCode.NotFound, missingDelete.StatusCode);
        }

        var tenantBAfterDeniedMutations = await tenantB.GetAsync(sharedName);
        Assert.Equal(SecretStatus.Active, tenantBAfterDeniedMutations.Status);
        Assert.Equal(1, tenantBAfterDeniedMutations.CurrentVersion);

        await tenantA.UpdateAsync(sharedName, new UpdateSecretRequest { DisplayName = "Tenant A only" });
        Assert.NotEqual("Tenant A only", (await tenantB.GetAsync(sharedName)).DisplayName);
        Assert.Equal(2, (await tenantA.RotateAsync(sharedName, new RotateSecretRequest { Value = "tenant-a-rotated" })).CurrentVersion);
        Assert.Equal(1, (await tenantB.GetAsync(sharedName)).CurrentVersion);
        await tenantA.RevokeAsync(sharedName);
        Assert.Equal(SecretStatus.Active, (await tenantB.GetAsync(sharedName)).Status);
        await tenantA.DeleteAsync(sharedName);
        Assert.Equal(secretB.Id, (await tenantB.GetAsync(sharedName)).Id);
        var tenantATestAfterDelete = await tenantA.TestAsync(sharedName);
        Assert.False(tenantATestAfterDelete.Succeeded);
        Assert.Contains("not found", tenantATestAfterDelete.Error, StringComparison.OrdinalIgnoreCase);
        Assert.True((await tenantB.TestAsync(sharedName)).Succeeded);

        AssertNoSecretMaterial(
            tenantACapture.ResponseBodies
                .Concat(tenantBCapture.ResponseBodies)
                .Concat(tenantCCapture.ResponseBodies)
                .Concat(defaultTenantCapture.ResponseBodies),
            "tenant-a-secret", "tenant-b-secret", "tenant-a-rotated");
    }

    [Theory]
    [MemberData(nameof(PermissionMatrix))]
    public async Task EachCanonicalOperationAdmitsOnlyPrincipalsHoldingItsCorePermission(string? permissions, string[] allowedOperations)
    {
        using var seeder = CreateClient(AllSecretsPermissions, "tenant-a", out _);
        var seeded = await RestService.For<ISecretsApi>(seeder).CreateAsync(new CreateSecretRequest
        {
            Name = MatrixSecretName,
            DisplayName = MatrixSeedDisplayName,
            Value = "matrix-seed-value"
        });

        using var principal = CreateClient(permissions, "tenant-a", out var capture);
        var deniedStatus = permissions is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
        var expected = new List<(string Operation, HttpStatusCode Status)>();
        var actual = new List<(string Operation, HttpStatusCode Status)>();
        foreach (var operation in MatrixOperations)
        {
            var allowed = allowedOperations.Contains(operation.Name);
            using var request = new HttpRequestMessage(operation.Method, operation.Path)
            {
                Content = operation.Body is null ? null : StudioSerializer.ToHttpContent(operation.Body)
            };
            using var response = await principal.SendAsync(request);
            expected.Add((operation.Name, allowed ? HttpStatusCode.OK : deniedStatus));
            actual.Add((operation.Name, response.StatusCode));
            if (allowed && response.IsSuccessStatusCode)
            {
                await AssertAdmittedResponseAsync(operation.Name, response.Content, seeded.Id);
            }
        }

        Assert.Equal(expected, actual);
        await AssertMatrixStateAsync(seeder, seeded.Id, allowedOperations);

        var mayDelete = allowedOperations.Contains("delete");
        using var deleted = await principal.DeleteAsync($"/secrets/{MatrixSecretName}");
        Assert.Equal(mayDelete ? HttpStatusCode.NoContent : deniedStatus, deleted.StatusCode);
        using var afterDelete = await seeder.GetAsync($"/secrets/{MatrixSecretName}");
        Assert.Equal(mayDelete ? HttpStatusCode.NotFound : HttpStatusCode.OK, afterDelete.StatusCode);

        AssertNoSecretMaterial(capture.ResponseBodies, "matrix-seed-value", "matrix-rotated-value", "matrix-created-value");
    }

    [Theory]
    [MemberData(nameof(CrossTenantPrincipals))]
    public async Task PrincipalResolvedToAnotherTenantCannotReadOrChangeTheSecret(string permissions, string? requestTenant)
    {
        const string name = "cross-tenant-authorization";
        using var owningTenant = CreateClient(AllSecretsPermissions, "tenant-b", out _);
        var owningTenantApi = RestService.For<ISecretsApi>(owningTenant);
        var secret = await owningTenantApi.CreateAsync(new CreateSecretRequest
        {
            Name = name,
            DisplayName = "Tenant B only",
            Value = "tenant-b-authorization-value"
        });

        // The same grant resolved to tenant B sees the secret, so the empty results below are not vacuous.
        using var sameTenant = CreateClient(permissions, "tenant-b", out _);
        var sameTenantApi = RestService.For<ISecretsApi>(sameTenant);
        Assert.Equal(secret.Id, (await sameTenantApi.GetAsync(name)).Id);
        Assert.Contains((await sameTenantApi.ListAsync()).Items, item => item.Id == secret.Id);
        Assert.Contains((await sameTenantApi.PickAsync(new SecretPickerRequest())).Items, item => item.Id == secret.Id);

        using var otherTenant = CreateClient(permissions, requestTenant, out var capture);
        var otherTenantApi = RestService.For<ISecretsApi>(otherTenant);
        Assert.DoesNotContain((await otherTenantApi.ListAsync()).Items, item => item.Id == secret.Id);
        Assert.DoesNotContain((await otherTenantApi.PickAsync(new SecretPickerRequest())).Items, item => item.Id == secret.Id);
        await AssertApiStatusAsync(HttpStatusCode.NotFound, () => otherTenantApi.GetAsync(name));

        // A view-only grant is refused before any lookup; a manage grant reaches the handler and finds nothing in its tenant.
        var canChange = permissions != "secrets:view";
        var refused = canChange ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden;
        await AssertApiStatusAsync(refused, () => otherTenantApi.UpdateAsync(name, new UpdateSecretRequest { DisplayName = "Must not cross tenants" }));
        await AssertApiStatusAsync(refused, () => otherTenantApi.RotateAsync(name, new RotateSecretRequest { Value = "must-not-cross-tenants" }));
        await AssertApiStatusAsync(refused, () => otherTenantApi.RevokeAsync(name));
        await AssertApiStatusAsync(refused, () => otherTenantApi.DeleteAsync(name));
        if (canChange)
        {
            var test = await otherTenantApi.TestAsync(name);
            Assert.False(test.Succeeded);
            Assert.Contains("not found", test.Error, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            await AssertApiStatusAsync(HttpStatusCode.Forbidden, () => otherTenantApi.TestAsync(name));
        }

        var after = await owningTenantApi.GetAsync(name);
        Assert.Equal((secret.Id, "Tenant B only", (int?)1, SecretStatus.Active), (after.Id, after.DisplayName, after.CurrentVersion, after.Status));
        Assert.True((await owningTenantApi.TestAsync(name)).Succeeded);
        AssertNoSecretMaterial(capture.ResponseBodies, "tenant-b-authorization-value", "must-not-cross-tenants");
    }

    [Fact]
    public async Task AnotherIdentityWithTheSameGrantManagesTheSecretBecauseCoreHasNoOwnerPredicate()
    {
        const string name = "shared-operator-secret";
        using var creator = CreateClient(AllSecretsPermissions, "tenant-a", out var creatorCapture, identity: "secret-creator");
        var created = await RestService.For<ISecretsApi>(creator).CreateAsync(new CreateSecretRequest { Name = name, Value = "creator-supplied-value" });

        using var colleague = CreateClient(AllSecretsPermissions, "tenant-a", out var colleagueCapture, identity: "second-operator");
        var api = RestService.For<ISecretsApi>(colleague);
        Assert.Equal(created.Id, (await api.GetAsync(name)).Id);
        Assert.Contains((await api.ListAsync()).Items, item => item.Id == created.Id);
        Assert.Equal("Renamed by a second operator", (await api.UpdateAsync(name, new UpdateSecretRequest { DisplayName = "Renamed by a second operator" })).DisplayName);
        Assert.Equal(2, (await api.RotateAsync(name, new RotateSecretRequest { Value = "rotated-by-second-operator" })).CurrentVersion);
        Assert.True((await api.TestAsync(name)).Succeeded);
        Assert.Equal(SecretStatus.Revoked, (await api.RevokeAsync(name)).Status);
        await api.DeleteAsync(name);

        // No Core response carries an owner field a legacy client could mistake for an authorization boundary.
        var bodies = creatorCapture.ResponseBodies.Concat(colleagueCapture.ResponseBodies).ToList();
        Assert.DoesNotContain(bodies, body => body.Contains("owner", StringComparison.OrdinalIgnoreCase));
        AssertNoSecretMaterial(bodies, "creator-supplied-value", "rotated-by-second-operator");
    }

    [Fact]
    public async Task LifecycleManagedGenerationsAreNotFoundThroughNameAddressedEndpoints()
    {
        using var client = CreateClient(AllSecretsPermissions, null, out var capture);
        var api = RestService.For<ISecretsApi>(client);
        const string ordinaryName = "ordinary-beside-managed";
        const string ownerId = "connection-managed-contract";
        const string generationId = "generation-managed-contract";
        const string managedValue = "managed-generation-never-echo";
        var ordinary = await api.CreateAsync(new CreateSecretRequest { Name = ordinaryName, Value = "ordinary-secret-never-echo" });

        string managedName;
        await using (var scope = _app!.Services.CreateAsyncScope())
            managedName = (await scope.ServiceProvider.GetRequiredService<IManagedSecretManager>().CreateGenerationAsync(ownerId, generationId, managedValue)).Name;

        Assert.DoesNotContain((await api.ListAsync()).Items, item => item.Name == managedName);
        Assert.Equal(ordinary.Id, (await api.GetAsync(ordinaryName)).Id);

        await AssertNotFoundAsync(() => api.GetAsync(managedName));
        await AssertNotFoundAsync(() => api.UpdateAsync(managedName, new UpdateSecretRequest { DisplayName = "Must not update" }));
        await AssertNotFoundAsync(() => api.RotateAsync(managedName, new RotateSecretRequest { Value = "must-not-rotate" }));
        await AssertNotFoundAsync(() => api.RevokeAsync(managedName));
        await AssertNotFoundAsync(() => api.DeleteAsync(managedName));

        var test = await api.TestAsync(managedName);
        Assert.False(test.Succeeded);
        Assert.Contains("not found", test.Error, StringComparison.OrdinalIgnoreCase);

        await using (var scope = _app!.Services.CreateAsyncScope())
        {
            var payload = await scope.ServiceProvider.GetRequiredService<IManagedSecretManager>().ResolveGenerationAsync(managedName, ownerId, generationId);
            Assert.Equal(managedValue, payload.Value);
        }

        Assert.Equal(ordinary.Id, (await api.GetAsync(ordinaryName)).Id);
        Assert.DoesNotContain(capture.ResponseBodies, body => body.Contains(ownerId, StringComparison.Ordinal) || body.Contains(generationId, StringComparison.Ordinal));
        AssertNoSecretMaterial(capture.ResponseBodies, managedValue, "ordinary-secret-never-echo", "must-not-rotate");
    }

    private static async Task AssertNotFoundAsync(Func<Task> call)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(call);
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    /// <summary>An admitted call must have returned the seeded secret or its effect, not merely a 200.</summary>
    private static async Task AssertAdmittedResponseAsync(string operation, HttpContent content, string secretId)
    {
        switch (operation)
        {
            case "list":
                Assert.Contains((await ReadAsync<ListSecretsResponse>(content)).Items, item => item.Id == secretId);
                break;
            case "descriptors":
                Assert.Contains((await ReadAsync<SecretDescriptorsResponse>(content)).Types, descriptor => descriptor.Name == SecretTypeNames.Text);
                break;
            case "get":
                Assert.Equal(secretId, (await ReadAsync<SecretModel>(content)).Id);
                break;
            case "picker":
                Assert.Contains((await ReadAsync<SecretPickerResponse>(content)).Items, item => item.Id == secretId);
                break;
            case "test":
                Assert.True((await ReadAsync<SecretTestResponse>(content)).Succeeded);
                break;
            case "rotate":
                Assert.Equal(2, (await ReadAsync<SecretModel>(content)).CurrentVersion);
                break;
        }
    }

    /// <summary>A refused call must not have half-run, and an admitted one must have taken effect.</summary>
    private static async Task AssertMatrixStateAsync(HttpClient seeder, string secretId, string[] allowedOperations)
    {
        var current = await RestService.For<ISecretsApi>(seeder).GetAsync(MatrixSecretName);
        Assert.Equal(secretId, current.Id);
        Assert.Equal(allowedOperations.Contains("update") ? MatrixUpdatedDisplayName : MatrixSeedDisplayName, current.DisplayName);
        Assert.Equal(allowedOperations.Contains("revoke") ? SecretStatus.Revoked : SecretStatus.Active, current.Status);
        // CurrentVersion is the latest active version, so a revoked secret reports none.
        int? expectedVersion = allowedOperations.Contains("revoke") ? null : allowedOperations.Contains("rotate") ? 2 : 1;
        Assert.Equal(expectedVersion, current.CurrentVersion);

        using var created = await seeder.GetAsync($"/secrets/{MatrixCreatedSecretName}");
        Assert.Equal(allowedOperations.Contains("create") ? HttpStatusCode.OK : HttpStatusCode.NotFound, created.StatusCode);
    }

    private static async Task<T> ReadAsync<T>(HttpContent content) => (await StudioSerializer.FromHttpContentAsync<T>(content))!;

    private static async Task AssertApiStatusAsync(HttpStatusCode expected, Func<Task> call) =>
        Assert.Equal(expected, (await Assert.ThrowsAnyAsync<ApiException>(call)).StatusCode);

    private HttpClient CreateClient(string? permissions, string? tenantId, out ResponseCaptureHandler capture, string identity = DefaultIdentity)
    {
        capture = new ResponseCaptureHandler(_app!.GetTestServer().CreateHandler());
        var client = new HttpClient(capture) { BaseAddress = new Uri("http://localhost") };
        if (permissions is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.PermissionHeader, permissions);
        }
        if (tenantId is not null)
        {
            client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        }
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.IdentityHeader, identity);
        return client;
    }

    private static void AssertNoSecretMaterial(IEnumerable<string> responseBodies, params string[] secretValues)
    {
        foreach (var value in secretValues)
        {
            Assert.DoesNotContain(responseBodies, body => body.Contains(value, StringComparison.Ordinal));
        }

        Assert.DoesNotContain(responseBodies, body => body.Contains("\"value\"", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(responseBodies, body => body.Contains("EncryptedValue", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(responseBodies, body => body.Contains("ProtectedValue", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record SecretOperation(string Name, HttpMethod Method, string Path, object? Body = null);

    private sealed class ResponseCaptureHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
    {
        public List<string> ResponseBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            ResponseBodies.Add(await response.Content.ReadAsStringAsync(cancellationToken));
            return response;
        }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationScheme = "SecretsContractTest";
        public const string PermissionHeader = "X-Test-Permissions";
        public const string IdentityHeader = "X-Test-Identity";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(PermissionHeader, out var permissionHeader))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = permissionHeader
                .SelectMany(value => value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
                .Select(permission => new Claim(PermissionNames.ClaimType, permission))
                .ToList();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, Request.Headers[IdentityHeader].FirstOrDefault() ?? "contract-test-user"));
            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), AuthenticationScheme)));
        }
    }
}

[CollectionDefinition(nameof(SecretsApiHttpCollection), DisableParallelization = true)]
public sealed class SecretsApiHttpCollection;
