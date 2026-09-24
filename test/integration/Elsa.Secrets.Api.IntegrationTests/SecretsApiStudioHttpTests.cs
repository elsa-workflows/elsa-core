using System.Net;
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
    private WebApplication? _app;
    private string? _databasePath;
    private bool _previousSecuritySetting;

    public async Task InitializeAsync()
    {
        _previousSecuritySetting = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = true;
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

        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
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
        }
    }

    [Fact]
    public async Task PinnedStudioRefitClientCompletesSecretOperationsWithoutReturningStoredValue()
    {
        using var client = CreateClient("secrets:view,secrets:write,secrets:delete,secrets:test", "tenant-a", out var capture);
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

        using var deniedDelete = await writer.DeleteAsync($"/secrets/{name}");
        Assert.Equal(HttpStatusCode.Forbidden, deniedDelete.StatusCode);

        using var reader = CreateClient("secrets:view", null, out var readerCapture);
        using var stillPresent = await reader.GetAsync($"/secrets/{name}");
        Assert.Equal(HttpStatusCode.OK, stillPresent.StatusCode);
        using var legacyInput = await reader.GetAsync($"/secrets/{name}/input");
        Assert.Equal(HttpStatusCode.NotFound, legacyInput.StatusCode);

        var viewOnlyApi = RestService.For<ISecretsApi>(reader);
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
    public async Task HttpTenantResolutionSeparatesSameNameSecretsAcrossTenants()
    {
        using var tenantAClient = CreateClient("secrets:view,secrets:write,secrets:delete,secrets:test", "tenant-a", out var tenantACapture);
        using var tenantBClient = CreateClient("secrets:view,secrets:write,secrets:test", "tenant-b", out var tenantBCapture);
        using var tenantCClient = CreateClient("secrets:view,secrets:write,secrets:delete,secrets:test", "tenant-c", out var tenantCCapture);
        using var defaultTenantClient = CreateClient("secrets:view,secrets:write,secrets:delete,secrets:test", null, out var defaultTenantCapture);
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
            var missingDelete = await Assert.ThrowsAsync<ApiException>(() => isolatedTenant.DeleteAsync(sharedName));
            Assert.Equal(HttpStatusCode.NotFound, missingDelete.StatusCode);
        }

        await tenantA.UpdateAsync(sharedName, new UpdateSecretRequest { DisplayName = "Tenant A only" });
        Assert.NotEqual("Tenant A only", (await tenantB.GetAsync(sharedName)).DisplayName);
        Assert.Equal(2, (await tenantA.RotateAsync(sharedName, new RotateSecretRequest { Value = "tenant-a-rotated" })).CurrentVersion);
        Assert.Equal(1, (await tenantB.GetAsync(sharedName)).CurrentVersion);
        await tenantA.RevokeAsync(sharedName);
        Assert.Equal(SecretStatus.Active, (await tenantB.GetAsync(sharedName)).Status);
        await tenantA.DeleteAsync(sharedName);
        Assert.Equal(secretB.Id, (await tenantB.GetAsync(sharedName)).Id);

        AssertNoSecretMaterial(
            tenantACapture.ResponseBodies
                .Concat(tenantBCapture.ResponseBodies)
                .Concat(tenantCCapture.ResponseBodies)
                .Concat(defaultTenantCapture.ResponseBodies),
            "tenant-a-secret", "tenant-b-secret", "tenant-a-rotated");
    }

    private HttpClient CreateClient(string? permissions, string? tenantId, out ResponseCaptureHandler capture)
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
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.IdentityHeader, "contract-test-user");
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
