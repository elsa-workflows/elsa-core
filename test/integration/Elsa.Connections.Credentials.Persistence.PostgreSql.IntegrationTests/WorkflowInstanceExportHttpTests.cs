using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Elsa;
using Elsa.Common.Multitenancy;
using Elsa.Common.Models;
using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Connections.Credentials.Persistence.EFCore.Features;
using Elsa.Connections.Credentials.Persistence.EFCore.Sqlite.Extensions;
using Elsa.Connections.Credentials.Workflows.Contracts;
using Elsa.Connections.Credentials.Workflows.Features;
using Elsa.Connections.Features;
using Elsa.Connections.Models;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Features;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore.Sqlite.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Api.Features;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Testing.Shared;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[CollectionDefinition("Connections HTTP export", DisableParallelization = true)]
public sealed class ConnectionsHttpExportCollection;

[Collection("Connections HTTP export")]
public sealed class WorkflowInstanceExportHttpTests
{
    private const string TenantId = "tenant-http-export";
    private const string EnvironmentId = "environment-http-export";
    private const string LogicalBindingId = "payments";
    private const string ApiKey = "synthetic-api-key-http-export";

    [Fact]
    public async Task ExportAfterPersistedApiKeyWorkflowResume_RedactsCredentialFromAllOptionalSections()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"elsa-http-export-{Guid.NewGuid():N}.db");
        var previousSecurityState = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = false;

        try
        {
            string workflowInstanceId;
            string resumeBookmarkId;
            string secondWorkflowInstanceId;
            string secondResumeBookmarkId;

            await using (var first = await TestHost.StartAsync(databasePath))
            {
                using var tenant = first.TenantAccessor.PushContext(new Tenant { Id = TenantId, Name = TenantId });
                await using var scope = first.App.Services.CreateAsyncScope();
                var services = scope.ServiceProvider;
                var principal = Principal();

                var connected = await services.GetRequiredService<IStaticApiKeyLifecycleService>().ConnectApiKeyAsync(
                    principal,
                    new ConnectApiKeyConnectionRequest(TenantId, EnvironmentId, "synthetic-api-key", "synthetic-account", ApiKey));
                Assert.True(connected.Succeeded, connected.SafeErrorCode);
                var connectionId = Assert.IsType<string>(connected.ConnectionId);

                var binding = await services.GetRequiredService<IWorkflowCredentialBindingManager>()
                    .CreateAsync(principal, LogicalBindingId, connectionId);
                Assert.True(binding.Succeeded, binding.SafeErrorCode);
                var bindingRevision = Assert.IsType<long>(binding.Revision);

                (workflowInstanceId, resumeBookmarkId) = await StartSuspendedWorkflowAsync(services);
                (secondWorkflowInstanceId, secondResumeBookmarkId) = await StartSuspendedWorkflowAsync(services);

                Assert.Empty(first.Probe.Credentials);
                var grant = await services.GetRequiredService<IWorkflowCredentialGrantManager>()
                    .IssueAsync(principal, workflowInstanceId, LogicalBindingId, bindingRevision);
                Assert.True(grant.Succeeded, grant.SafeErrorCode);
                var secondGrant = await services.GetRequiredService<IWorkflowCredentialGrantManager>()
                    .IssueAsync(principal, secondWorkflowInstanceId, LogicalBindingId, bindingRevision);
                Assert.True(secondGrant.Succeeded, secondGrant.SafeErrorCode);

                var persisted = await services.GetRequiredService<IWorkflowInstanceStore>().FindAsync(workflowInstanceId);
                Assert.NotNull(persisted);
                Assert.DoesNotContain(ApiKey, JsonSerializer.Serialize(persisted), StringComparison.Ordinal);
            }

            await using (var second = await TestHost.StartAsync(databasePath))
            {
                using var tenant = second.TenantAccessor.PushContext(new Tenant { Id = TenantId, Name = TenantId });
                await using var scope = second.App.Services.CreateAsyncScope();
                var services = scope.ServiceProvider;

                var runtime = services.GetRequiredService<IWorkflowRuntime>();
                var client = await runtime.CreateClientAsync(workflowInstanceId);
                await client.RunInstanceAsync(new RunWorkflowInstanceRequest { BookmarkId = resumeBookmarkId });
                client = await runtime.CreateClientAsync(secondWorkflowInstanceId);
                await client.RunInstanceAsync(new RunWorkflowInstanceRequest { BookmarkId = secondResumeBookmarkId });

                Assert.Equal(2, second.Probe.Credentials.Count);
                Assert.All(second.Probe.Credentials, credential => Assert.Equal(ApiKey, credential));
                var response = await second.HttpClient.GetAsync(
                    $"/workflow-instances/{workflowInstanceId}/export?includeBookmarks=true&includeActivityExecutionLog=true&includeWorkflowExecutionLog=true");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var body = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain(ApiKey, body, StringComparison.Ordinal);
                using var document = JsonDocument.Parse(body);
                var exported = document.RootElement;
                Assert.True(exported.GetProperty("Bookmarks").GetArrayLength() > 0);
                Assert.True(exported.GetProperty("ActivityExecutionRecords").GetArrayLength() > 0);
                Assert.True(exported.GetProperty("WorkflowExecutionLogRecords").GetArrayLength() > 0);

                var postResponse = await second.HttpClient.PostAsJsonAsync(
                    $"/workflow-instances/{workflowInstanceId}/export",
                    new { includeBookmarks = true, includeActivityExecutionLog = true, includeWorkflowExecutionLog = true });
                Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
                Assert.Equal(workflowInstanceId, AssertExportBodyExcludesCredential(await postResponse.Content.ReadAsStringAsync()));

                var bulkResponse = await second.HttpClient.PostAsJsonAsync(
                    "/bulk-actions/export/workflow-instances",
                    new
                    {
                        ids = new[] { workflowInstanceId, secondWorkflowInstanceId },
                        includeBookmarks = true,
                        includeActivityExecutionLog = true,
                        includeWorkflowExecutionLog = true
                    });
                Assert.Equal(HttpStatusCode.OK, bulkResponse.StatusCode);
                await using (var zipStream = await bulkResponse.Content.ReadAsStreamAsync())
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                {
                    Assert.Equal(2, archive.Entries.Count);
                    var exportedIds = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var entry in archive.Entries)
                    {
                        using var reader = new StreamReader(entry.Open());
                        Assert.True(exportedIds.Add(AssertExportBodyExcludesCredential(await reader.ReadToEndAsync())));
                    }
                    Assert.Equal(new[] { workflowInstanceId, secondWorkflowInstanceId }.Order(), exportedIds.Order());
                }

                await using var database = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>()
                    .CreateDbContextAsync();
                var connection = database.Database.GetDbConnection();
                await connection.OpenAsync();
                foreach (var instanceId in new[] { workflowInstanceId, secondWorkflowInstanceId })
                {
                    var persisted = await services.GetRequiredService<IWorkflowInstanceStore>().FindAsync(instanceId);
                    Assert.NotNull(persisted);
                    Assert.DoesNotContain(ApiKey, JsonSerializer.Serialize(persisted), StringComparison.Ordinal);

                    await using var command = connection.CreateCommand();
                    command.CommandText = "SELECT Data FROM WorkflowInstances WHERE Id = @id";
                    var idParameter = command.CreateParameter();
                    idParameter.ParameterName = "@id";
                    idParameter.Value = instanceId;
                    command.Parameters.Add(idParameter);
                    var rawState = Assert.IsType<string>(await command.ExecuteScalarAsync());
                    Assert.DoesNotContain(ApiKey, rawState, StringComparison.Ordinal);
                }
            }
        }
        finally
        {
            EndpointSecurityOptions.SecurityIsEnabled = previousSecurityState;
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    private static async Task<(string WorkflowInstanceId, string BookmarkId)> StartSuspendedWorkflowAsync(IServiceProvider services)
    {
        var client = await services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
        var started = await client.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(
                ApiKeyExportWorkflow.DefinitionId, VersionOptions.Latest)
        });
        var bookmark = Assert.Single(await services.GetRequiredService<IBookmarkStore>()
            .FindManyAsync(new BookmarkFilter { WorkflowInstanceId = started.WorkflowInstanceId }));
        return (started.WorkflowInstanceId, bookmark.Id);
    }

    private static string AssertExportBodyExcludesCredential(string body)
    {
        Assert.DoesNotContain(ApiKey, body, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(body);
        var exported = document.RootElement;
        Assert.True(exported.GetProperty("Bookmarks").GetArrayLength() > 0);
        Assert.True(exported.GetProperty("ActivityExecutionRecords").GetArrayLength() > 0);
        Assert.True(exported.GetProperty("WorkflowExecutionLogRecords").GetArrayLength() > 0);
        return exported.GetProperty("WorkflowState").GetProperty("id").GetString()!;
    }

    private static ClaimsPrincipal Principal() => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, "http-export-test")], "synthetic"));

    private sealed class TestHost(WebApplication app, DefaultTenantAccessor tenantAccessor, ExportCredentialProbe probe) : IAsyncDisposable
    {
        public WebApplication App { get; } = app;
        public HttpClient HttpClient { get; } = app.GetTestClient();
        public DefaultTenantAccessor TenantAccessor { get; } = tenantAccessor;
        public ExportCredentialProbe Probe { get; } = probe;

        public static async Task<TestHost> StartAsync(string databasePath)
        {
            var connectionString = $"Data Source={databasePath};Cache=Shared;Pooling=False;";
            var tenantAccessor = new DefaultTenantAccessor();
            var probe = new ExportCredentialProbe();
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddAuthorization();
            builder.Services.AddSingleton(tenantAccessor);
            builder.Services.AddSingleton<ITenantAccessor>(tenantAccessor);
            builder.Services.AddSingleton(probe);
            builder.Services.AddSingleton<IConnectionUseAuthorizer, AllowConnectionUseAuthorizer>();
            builder.Services.AddSingleton<IConnectionCredentialBindingManagementAuthorizer, AllowBindingManagement>();
            builder.Services.AddSingleton<IConnectionCredentialGrantManagementAuthorizer, AllowGrantManagement>();
            builder.Services.AddSingleton<IConnectionCredentialShareAuthorizer, AllowWorkflowShare>();
            builder.Services.AddSingleton<IConnectionCredentialProvider, UnusedCredentialProvider>();
            builder.Services.AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(WorkflowsApiFeature).Assembly];
                options.DisableAutoDiscovery = true;
            });
            builder.Services.AddElsa(elsa =>
            {
                var secrets = elsa.Configure<SecretsFeature>();
                secrets.ConfigureOptions = options => options.EncryptionKey = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
                secrets.UseEntityFrameworkCore(feature => feature.UseSqlite(connectionString));
                elsa.Configure<ConnectionsFeature>();
                elsa.Configure<EFCoreConnectionsPersistenceFeature>(feature => feature.UseSqlite(connectionString));
                elsa.Configure<WorkflowCredentialBindingsFeature>(feature => feature.EnvironmentId = EnvironmentId);
                elsa.Configure<WorkflowCredentialUseGrantsFeature>();
                elsa.AddWorkflow<ApiKeyExportWorkflow>();
                elsa.AddActivity<ApiKeyExportActivity>();
                elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(feature => feature.UseSqlite(connectionString)));
                elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(feature => feature.UseSqlite(connectionString)));
                elsa.UseWorkflowsApi();
            });

            var app = builder.Build();
            app.UseAuthorization();
            app.UseFastEndpoints();
            await app.StartAsync();
            await MigrateAsync(app.Services);
            await app.Services.GetRequiredService<IRegistriesPopulator>().PopulateAsync();
            return new TestHost(app, tenantAccessor, probe);
        }

        private static async Task MigrateAsync(IServiceProvider services)
        {
            await using var scope = services.CreateAsyncScope();
            await using var connections = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>().CreateDbContextAsync();
            await connections.Database.MigrateAsync();
            await using var secrets = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>().CreateDbContextAsync();
            await secrets.Database.MigrateAsync();
            await using var management = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();
            await management.Database.MigrateAsync();
            await using var runtime = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<RuntimeElsaDbContext>>().CreateDbContextAsync();
            await runtime.Database.MigrateAsync();
        }

        public async ValueTask DisposeAsync()
        {
            HttpClient.Dispose();
            await App.StopAsync();
            await App.DisposeAsync();
        }
    }

    public sealed class ExportCredentialProbe
    {
        public List<string> Credentials { get; } = [];
    }

    private sealed class AllowConnectionUseAuthorizer : IConnectionUseAuthorizer
    {
        public Task<bool> AuthorizeAsync(ConnectionUseRequest request, CancellationToken cancellationToken = default) => Task.FromResult(
            request.TenantId == TenantId && request.EnvironmentId == EnvironmentId);
    }

    private sealed class AllowBindingManagement : IConnectionCredentialBindingManagementAuthorizer
    {
        public Task<bool> AuthorizeAsync(ClaimsPrincipal principal, ConnectionCredentialBindingManagementRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(principal.Identity?.IsAuthenticated == true && request.TenantId == TenantId && request.EnvironmentId == EnvironmentId);
    }

    private sealed class AllowGrantManagement : IConnectionCredentialGrantManagementAuthorizer
    {
        public Task<bool> AuthorizeAsync(ClaimsPrincipal principal, ConnectionCredentialGrantManagementRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(principal.Identity?.IsAuthenticated == true && request.TenantId == TenantId && request.EnvironmentId == EnvironmentId);
    }

    private sealed class AllowWorkflowShare : IConnectionCredentialShareAuthorizer
    {
        public Task<bool> AuthorizeAsync(ClaimsPrincipal principal, ConnectionCredentialShareRequest request,
            CancellationToken cancellationToken = default) => Task.FromResult(
            principal.Identity?.IsAuthenticated == true && request.TenantId == TenantId &&
            request.EnvironmentId == EnvironmentId && request.BindingRevision > 0);
    }

    private sealed class UnusedCredentialProvider : IConnectionCredentialProvider
    {
        public Task<CredentialMaterial> RefreshAsync(string providerId, string accountId, string refreshToken, CancellationToken cancellationToken = default) =>
            Task.FromException<CredentialMaterial>(new InvalidOperationException("The HTTP export API-key fixture must not refresh OAuth credentials."));
    }
}

public sealed class ApiKeyExportWorkflow : WorkflowBase
{
    public const string DefinitionId = "api-key-export-workflow";

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.Root = new Sequence
        {
            Activities =
            {
                new Event("Resume") { Id = "Resume" },
                new ApiKeyExportActivity(),
                new Event("AfterCredential") { Id = "AfterCredential" }
            }
        };
    }
}

public sealed class ApiKeyExportActivity : CodeActivity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var credential = await context.GetRequiredService<IWorkflowCredentialResolver>()
            .ResolveAsync(context.WorkflowExecutionContext, "payments");
        context.GetRequiredService<WorkflowInstanceExportHttpTests.ExportCredentialProbe>().Credentials.Add(credential.AccessToken);
    }
}
