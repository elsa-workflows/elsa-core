using System.Security.Claims;
using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Connections.Credentials.Persistence.EFCore.Features;
using Elsa.Connections.Credentials.Persistence.EFCore.Sqlite.Extensions;
using Elsa.Connections.Credentials.Workflows.Contracts;
using Elsa.Connections.Credentials.Workflows.Features;
using Elsa.Connections.Credentials.Workflows.Services;
using Elsa.Connections.Features;
using Elsa.Connections.Models;
using Elsa.Connections.Services;
using Elsa.Extensions;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Secrets.Models;
using Elsa.Tenants.Options;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Tasks;
using Elsa.Workflows.State;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Connections.UnitTests;

public sealed class WorkflowCredentialBindingTests
{
    private const string TenantId = "tenant-a";
    private const string EnvironmentId = "integration-test";
    private const string LogicalBindingId = "payments";

    [Fact]
    public async Task Resolve_UsesAmbientTenantAndConfiguredEnvironment_NotWorkflowInputs()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        await worker.SeedConnectionAsync("connection-other-tenant", "tenant-b", EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        var binding = await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a");
        Assert.True(binding.Succeeded);
        Assert.Equal(1, binding.Revision);

        var activityContext = await CreateActivityContextAsync("workflow-1", includeForgedInput: true);
        using (activityContext)
        {
            var credential = await scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>()
                .ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId);

            Assert.Equal("access-connection-a", credential.AccessToken);
            Assert.DoesNotContain("access-connection-a", JsonSerializer.Serialize(credential));
            var request = Assert.IsType<ConnectionCredentialBindingUseRequest>(worker.Authorizers.LastUseRequest);
            Assert.Equal(TenantId, request.TenantId);
            Assert.Equal(EnvironmentId, request.EnvironmentId);
            Assert.Equal("connection-a", request.ConnectionId);
            Assert.Equal(1, request.BindingRevision);
            Assert.Equal("workflow-1", request.WorkflowInstanceId);
            Assert.Equal((TenantId, EnvironmentId, "connection-a"), worker.CredentialService.LastRequest);
        }
    }

    [Fact]
    public async Task Resolve_RejectsConcurrentRebindBeforeAdmission_AndUsesNewBindingOnNextCall()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        await worker.SeedConnectionAsync("connection-b", TenantId, EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        Assert.True((await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a")).Succeeded);
        var activityContext = await CreateActivityContextAsync("workflow-race");
        using (activityContext)
        {
            worker.Authorizers.BeforeUseAuthorization = async request =>
            {
                Assert.Equal("connection-a", request.ConnectionId);
                var rebind = await manager.RebindAsync(Principal(), LogicalBindingId, request.BindingRevision, "connection-b");
                Assert.True(rebind.Succeeded);
                Assert.Equal(2, rebind.Revision);
            };

            await Assert.ThrowsAsync<ConnectionUnavailableException>(() => scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>()
                .ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId));
            Assert.Equal(0, worker.CredentialService.CallCount);

            worker.Authorizers.BeforeUseAuthorization = null;
            var next = await scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>()
                .ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId);

            Assert.Equal("access-connection-b", next.AccessToken);
            Assert.Equal((TenantId, EnvironmentId, "connection-b"), worker.CredentialService.LastRequest);
            Assert.Equal(2, worker.Authorizers.LastUseRequest!.BindingRevision);

            var staleRebind = await manager.RebindAsync(Principal(), LogicalBindingId, expectedRevision: 1, "connection-a");
            Assert.False(staleRebind.Succeeded);
            Assert.Equal("connection_unavailable", staleRebind.SafeErrorCode);
        }
    }

    [Fact]
    public async Task Resolve_DoesNotFindAnotherTenantsLogicalBinding()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        using var scope = worker.Services.CreateScope();
        using (worker.TenantAccessor.PushContext(TenantContext()))
        {
            var manager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
            Assert.True((await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a")).Succeeded);
        }

        using (worker.TenantAccessor.PushContext(new Tenant { Id = "tenant-b", Name = "tenant-b" }))
        {
            var activityContext = await CreateActivityContextAsync("workflow-cross-tenant");
            using (activityContext)
            {
                await Assert.ThrowsAsync<ConnectionUnavailableException>(() => scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>()
                    .ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId));
                Assert.Null(worker.Authorizers.LastUseRequest);
                Assert.Equal(0, worker.CredentialService.CallCount);
            }
        }
    }

    [Fact]
    public async Task ConcurrentWorkersCreatingSameLogicalBindingReturnOneConflictWithoutOverwriting()
    {
        var insertBarrier = new BindingInsertBarrierInterceptor(workerCount: 2);
        await using var firstWorker = await Worker.CreateAsync(EnvironmentId, allow: true, saveChangesInterceptor: insertBarrier);
        await using var secondWorker = await Worker.CreateForDatabaseAsync(
            firstWorker.DatabasePath,
            EnvironmentId,
            allow: true,
            deleteDatabaseOnDispose: false,
            saveChangesInterceptor: insertBarrier);
        await firstWorker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        await firstWorker.SeedConnectionAsync("connection-b", TenantId, EnvironmentId);

        using var firstTenant = firstWorker.TenantAccessor.PushContext(TenantContext());
        using var secondTenant = secondWorker.TenantAccessor.PushContext(TenantContext());
        using var firstScope = firstWorker.Services.CreateScope();
        using var secondScope = secondWorker.Services.CreateScope();
        var firstManager = firstScope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        var secondManager = secondScope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<ConnectionCredentialBindingResult> CreateAfterStartAsync(
            IWorkflowCredentialBindingManager manager,
            string connectionId)
        {
            await start.Task;
            return await manager.CreateAsync(Principal(), LogicalBindingId, connectionId);
        }

        var firstCreate = CreateAfterStartAsync(firstManager, "connection-a");
        var secondCreate = CreateAfterStartAsync(secondManager, "connection-b");
        start.SetResult(true);
        var results = await Task.WhenAll(firstCreate, secondCreate);

        Assert.Equal(2, insertBarrier.ArrivalCount);
        Assert.Equal(1, Assert.Single(results, result => result.Succeeded).Revision);
        var rejected = Assert.Single(results, result => !result.Succeeded);
        Assert.Equal("connection_unavailable", rejected.SafeErrorCode);
        var stored = await firstScope.ServiceProvider.GetRequiredService<IConnectionCredentialBindingStore>()
            .FindAsync(TenantId, EnvironmentId, LogicalBindingId);
        Assert.NotNull(stored);
        Assert.Equal(1, stored.Revision);
        Assert.Contains(stored.ConnectionId, new[] { "connection-a", "connection-b" });

        var overflow = await firstManager.RebindAsync(Principal(), LogicalBindingId, long.MaxValue, "connection-a");
        Assert.False(overflow.Succeeded);
    }

    [Fact]
    public async Task Create_RethrowsUnrelatedDatabaseUpdateFailures()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        await using (var db = await worker.Services.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>().CreateDbContextAsync())
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER RejectCredentialBinding BEFORE INSERT ON ConnectionCredentialBindings BEGIN SELECT RAISE(ABORT, 'synthetic database failure'); END;");
        }

        using var scope = worker.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IConnectionCredentialBindingStore>();
        await Assert.ThrowsAsync<DbUpdateException>(() => store.TryCreateAsync(
            TenantId,
            EnvironmentId,
            LogicalBindingId,
            "connection-a"));
    }

    [Fact]
    public async Task RebindAfterAuthorizationRecheck_AllowsAdmittedCallToFinishWithSnapshot()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        await worker.SeedConnectionAsync("connection-b", TenantId, EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        Assert.True((await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a")).Succeeded);
        var activityContext = await CreateActivityContextAsync("workflow-admitted");
        using (activityContext)
        {
            worker.CredentialService.BeforeResolve = async request =>
            {
                Assert.Equal("connection-a", request.ConnectionId);
                var rebind = await manager.RebindAsync(Principal(), LogicalBindingId, 1, "connection-b");
                Assert.True(rebind.Succeeded);
            };

            var admitted = await scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>()
                .ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId);
            Assert.Equal("access-connection-a", admitted.AccessToken);

            worker.CredentialService.BeforeResolve = null;
            var subsequent = await scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>()
                .ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId);
            Assert.Equal("access-connection-b", subsequent.AccessToken);
        }
    }

    [Fact]
    public async Task MissingEnvironmentOrDefaultAuthorizationFailsClosed()
    {
        await using var worker = await Worker.CreateAsync(environmentId: null, allow: false);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        var result = await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a");

        Assert.False(result.Succeeded);
        Assert.Equal("connection_unavailable", result.SafeErrorCode);
        Assert.Equal(0, worker.Authorizers.ManagementCallCount);

        var activityContext = await CreateActivityContextAsync("workflow-denied");
        using (activityContext)
        {
            var exception = await Assert.ThrowsAsync<ConnectionUnavailableException>(() => scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>()
                .ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId));
            Assert.Equal("The connection is unavailable.", exception.Message);
            Assert.Equal(0, worker.CredentialService.CallCount);
        }
    }

    [Fact]
    public async Task DefaultAndAgnosticTenantContextsCannotCreateBindings()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        using var scope = worker.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        var resolver = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>();
        var activityContext = await CreateActivityContextAsync("workflow-unscoped");
        using (activityContext)
        {
            var defaultTenantResult = await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a");
            Assert.False(defaultTenantResult.Succeeded);
            await Assert.ThrowsAsync<ConnectionUnavailableException>(() => resolver.ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId));
            Assert.Equal(0, worker.Authorizers.ManagementCallCount);
            Assert.Null(worker.Authorizers.LastUseRequest);
            Assert.Equal(0, worker.CredentialService.CallCount);
        }

        using (worker.TenantAccessor.PushContext(new Tenant { Id = Tenant.AgnosticTenantId, Name = Tenant.AgnosticTenantId }))
        {
            var agnosticTenantResult = await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a");
            Assert.False(agnosticTenantResult.Succeeded);
            await Assert.ThrowsAsync<ConnectionUnavailableException>(() => resolver.ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId));
            Assert.Equal(0, worker.Authorizers.ManagementCallCount);
            Assert.Null(worker.Authorizers.LastUseRequest);
            Assert.Equal(0, worker.CredentialService.CallCount);
        }
    }

    [Fact]
    public async Task SourceEnvironmentBindingDoesNotAuthorizeTargetUntilExplicitTargetBinding()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true);
        await worker.SeedConnectionAsync("source-connection", TenantId, "source-environment");
        await worker.SeedConnectionAsync("target-connection", TenantId, EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IConnectionCredentialBindingStore>();
        Assert.NotNull(await store.TryCreateAsync(TenantId, "source-environment", LogicalBindingId, "source-connection"));
        var activityContext = await CreateActivityContextAsync("workflow-imported");
        using (activityContext)
        {
            var resolver = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>();
            await Assert.ThrowsAsync<ConnectionUnavailableException>(() => resolver.ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId));
            Assert.Equal(0, worker.CredentialService.CallCount);

            var targetBinding = await scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>()
                .CreateAsync(Principal(), LogicalBindingId, "target-connection");
            Assert.True(targetBinding.Succeeded);
            Assert.Equal(1, targetBinding.Revision);

            var targetCredential = await resolver.ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId);
            Assert.Equal("access-target-connection", targetCredential.AccessToken);
            Assert.Equal((TenantId, EnvironmentId, "target-connection"), worker.CredentialService.LastRequest);
        }
    }

    [Fact]
    public async Task FeatureAuthorizersDefaultToDeny()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        module.Configure<ConnectionsFeature>();
        module.Configure<WorkflowCredentialBindingsFeature>(feature => feature.EnvironmentId = EnvironmentId);
        module.Apply();
        await using var provider = services.BuildServiceProvider();

        Assert.IsType<DenyAllConnectionCredentialBindingUseAuthorizer>(provider.GetRequiredService<IConnectionCredentialBindingUseAuthorizer>());
        Assert.IsType<DenyAllConnectionCredentialBindingManagementAuthorizer>(provider.GetRequiredService<IConnectionCredentialBindingManagementAuthorizer>());
    }

    [Fact]
    public async Task PersistedWorkflowRestart_RestoresTenantBeforeResolvingLogicalBinding()
    {
        const string workflowInstanceId = "persisted-workflow-1";
        var now = DateTimeOffset.Parse("2026-09-23T12:00:00Z");
        string databasePath;
        await using (var originalWorker = await Worker.CreateAsync(EnvironmentId, allow: true, deleteDatabaseOnDispose: false))
        {
            databasePath = originalWorker.DatabasePath;
            await originalWorker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
            using var tenant = originalWorker.TenantAccessor.PushContext(TenantContext());
            using var originalScope = originalWorker.Services.CreateScope();
            var manager = originalScope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
            Assert.True((await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a")).Succeeded);

            var instances = originalScope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
            await instances.SaveAsync(new WorkflowInstance
            {
                Id = workflowInstanceId,
                TenantId = TenantId,
                DefinitionId = "payments-workflow",
                DefinitionVersionId = "payments-workflow-v1",
                Version = 1,
                Status = WorkflowStatus.Running,
                SubStatus = WorkflowSubStatus.Executing,
                IsExecuting = true,
                CreatedAt = now.AddHours(-1),
                UpdatedAt = now.AddMinutes(-20),
                WorkflowState = new WorkflowState
                {
                    Id = workflowInstanceId,
                    DefinitionId = "payments-workflow",
                    DefinitionVersionId = "payments-workflow-v1",
                    DefinitionVersion = 1,
                    Status = WorkflowStatus.Running,
                    SubStatus = WorkflowSubStatus.Executing,
                    IsExecuting = true
                }
            });
        }

        await using var worker = await Worker.CreateForDatabaseAsync(databasePath, EnvironmentId, allow: true, deleteDatabaseOnDispose: true);
        using var scope = worker.Services.CreateScope();
        var instancesStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var persistedInstance = await instancesStore.FindAsync(workflowInstanceId);
        Assert.NotNull(persistedInstance);
        var persistedJson = JsonSerializer.Serialize(persistedInstance);
        Assert.DoesNotContain("access-connection-a", persistedJson, StringComparison.Ordinal);
        Assert.DoesNotContain("generation", persistedJson, StringComparison.OrdinalIgnoreCase);

        string? observedTenantId = null;
        string? resolvedToken = null;
        var restarter = Substitute.For<IWorkflowRestarter>();
        restarter.RestartWorkflowAsync(workflowInstanceId, Arg.Any<CancellationToken>())
            .Returns(_ => ResolveDuringRestartAsync());

        async Task ResolveDuringRestartAsync()
        {
            observedTenantId = worker.TenantAccessor.TenantId;
            var activityContext = await CreateActivityContextAsync(workflowInstanceId);
            using (activityContext)
            {
                var resolver = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>();
                resolvedToken = (await resolver.ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId)).AccessToken;
            }
        }

        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(now);
        var tenantService = Substitute.For<ITenantService>();
        tenantService.FindAsync(TenantId, Arg.Any<CancellationToken>()).Returns(new Tenant { Id = TenantId, Name = TenantId });
        var task = new RestartInterruptedWorkflowsTask(
            restarter,
            instancesStore,
            scope.ServiceProvider.GetRequiredService<ILogger<RestartInterruptedWorkflowsTask>>(),
            Options.Create(new RuntimeOptions { InactivityThreshold = TimeSpan.FromMinutes(5), RestartInterruptedWorkflowsBatchSize = 10 }),
            clock,
            tenantService,
            worker.TenantAccessor);

        Assert.Equal(string.Empty, worker.TenantAccessor.TenantId);
        await task.ExecuteAsync(CancellationToken.None);

        Assert.Equal(TenantId, observedTenantId);
        Assert.Equal("access-connection-a", resolvedToken);
        Assert.Equal(string.Empty, worker.TenantAccessor.TenantId);
        Assert.Equal(TenantId, worker.Authorizers.LastUseRequest!.TenantId);
    }

    private static async Task<ActivityExecutionContext> CreateActivityContextAsync(string id, bool includeForgedInput = false)
    {
        var fixture = new ActivityTestFixture(new WriteLine("credential binding test"));
        if (includeForgedInput)
        {
            fixture.ConfigureContext(context =>
            {
                context.WorkflowExecutionContext.Input["tenantId"] = "tenant-b";
                context.WorkflowExecutionContext.Input["environmentId"] = "source-env";
                context.WorkflowExecutionContext.Input["connectionId"] = "connection-other-tenant";
                context.WorkflowExecutionContext.Properties["tenantId"] = "tenant-b";
            });
        }

        var activityContext = await fixture.BuildAsync();
        activityContext.WorkflowExecutionContext.Id = id;
        return activityContext;
    }

    private static ClaimsPrincipal Principal() => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "test-user")], "synthetic"));

    private static Tenant TenantContext() => new() { Id = TenantId, Name = TenantId };

    private sealed class Worker(ServiceProvider services, string databasePath, DefaultTenantAccessor tenantAccessor, TestBindingAuthorizers authorizers, TestCredentialService credentialService) : IAsyncDisposable
    {
        public ServiceProvider Services { get; } = services;
        public string DatabasePath { get; } = databasePath;
        public DefaultTenantAccessor TenantAccessor { get; } = tenantAccessor;
        public TestBindingAuthorizers Authorizers { get; } = authorizers;
        public TestCredentialService CredentialService { get; } = credentialService;
        private bool DeleteDatabaseOnDispose { get; init; } = true;

        public static Task<Worker> CreateAsync(
            string? environmentId,
            bool allow,
            bool deleteDatabaseOnDispose = true,
            SaveChangesInterceptor? saveChangesInterceptor = null)
        {
            var path = Path.Combine(Path.GetTempPath(), $"elsa-workflow-credential-binding-{Guid.NewGuid():N}.db");
            return CreateForDatabaseAsync(path, environmentId, allow, deleteDatabaseOnDispose, saveChangesInterceptor);
        }

        public static async Task<Worker> CreateForDatabaseAsync(
            string path,
            string? environmentId,
            bool allow,
            bool deleteDatabaseOnDispose,
            SaveChangesInterceptor? saveChangesInterceptor = null)
        {
            var connectionString = $"Data Source={path};Cache=Shared;Pooling=False;";
            var tenantAccessor = new DefaultTenantAccessor();
            var authorizers = new TestBindingAuthorizers(allow);
            var credentialService = new TestCredentialService();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ITenantAccessor>(tenantAccessor);
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.Configure<TenantsOptions>(options => options.IsEnabled = false);

            var module = services.CreateModule();
            module.Configure<ConnectionsFeature>();
            module.Configure<EFCoreConnectionsPersistenceFeature>(feature =>
            {
                feature.UseSqlite(connectionString);
                if (saveChangesInterceptor != null)
                {
                    var configureDbContext = feature.DbContextOptionsBuilder;
                    feature.DbContextOptionsBuilder = (provider, optionsBuilder) =>
                    {
                        configureDbContext(provider, optionsBuilder);
                        optionsBuilder.AddInterceptors(saveChangesInterceptor);
                    };
                }
            });
            module.Configure<WorkflowManagementFeature>();
            module.Configure<EFCoreWorkflowInstancePersistenceFeature>(feature => feature.UseSqlite(connectionString));
            module.Configure<WorkflowCredentialBindingsFeature>(feature => feature.EnvironmentId = environmentId);
            module.Apply();

            services.AddSingleton<IConnectionCredentialBindingUseAuthorizer>(authorizers);
            services.AddSingleton<IConnectionCredentialBindingManagementAuthorizer>(authorizers);
            services.AddSingleton<IConnectionBackgroundUseService>(credentialService);
            var serviceProvider = services.BuildServiceProvider();

            await using (var context = await serviceProvider.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>().CreateDbContextAsync())
            {
                await context.Database.MigrateAsync();
            }
            await using (var context = await serviceProvider.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            {
                await context.Database.MigrateAsync();
            }

            return new Worker(serviceProvider, path, tenantAccessor, authorizers, credentialService)
            {
                DeleteDatabaseOnDispose = deleteDatabaseOnDispose
            };
        }

        public async Task SeedConnectionAsync(string connectionId, string tenantId, string environmentId)
        {
            using var scope = Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>().CreateAsync(new IntegrationConnection
            {
                Id = connectionId,
                TenantId = tenantId,
                EnvironmentId = environmentId,
                ProviderId = "synthetic",
                ProviderAccountId = $"account-{connectionId}",
                Status = ConnectionStatus.Active,
                Revision = 1,
                OperationStatus = CredentialOperationStatus.None
            });
        }

        public async ValueTask DisposeAsync()
        {
            await Services.DisposeAsync();
            if (DeleteDatabaseOnDispose && File.Exists(DatabasePath))
            {
                File.Delete(DatabasePath);
            }
        }
    }

    private sealed class BindingInsertBarrierInterceptor(int workerCount) : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _allWorkersReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _remainingWorkers = workerCount;
        private int _arrivalCount;

        public int ArrivalCount => Volatile.Read(ref _arrivalCount);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var isAddingBinding = eventData.Context?.ChangeTracker.Entries<ConnectionCredentialBinding>()
                .Any(entry => entry.State == EntityState.Added) == true;
            if (!isAddingBinding)
            {
                return result;
            }

            Interlocked.Increment(ref _arrivalCount);
            if (Interlocked.Decrement(ref _remainingWorkers) == 0)
            {
                _allWorkersReady.TrySetResult();
            }

            await _allWorkersReady.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            return result;
        }
    }

    private sealed class TestBindingAuthorizers(bool allow) : IConnectionCredentialBindingUseAuthorizer, IConnectionCredentialBindingManagementAuthorizer
    {
        public int ManagementCallCount { get; private set; }
        public ConnectionCredentialBindingUseRequest? LastUseRequest { get; private set; }
        public Func<ConnectionCredentialBindingUseRequest, Task>? BeforeUseAuthorization { get; set; }

        public async Task<bool> AuthorizeAsync(ConnectionCredentialBindingUseRequest request, CancellationToken cancellationToken = default)
        {
            LastUseRequest = request;
            if (BeforeUseAuthorization != null)
            {
                await BeforeUseAuthorization(request);
            }

            return allow;
        }

        public Task<bool> AuthorizeAsync(ClaimsPrincipal principal, ConnectionCredentialBindingManagementRequest request, CancellationToken cancellationToken = default)
        {
            ManagementCallCount++;
            return Task.FromResult(allow && principal.Identity?.IsAuthenticated == true && request.TenantId == TenantId && request.EnvironmentId == EnvironmentId);
        }
    }

    private sealed class TestCredentialService : IConnectionBackgroundUseService
    {
        public int CallCount { get; private set; }
        public (string TenantId, string EnvironmentId, string ConnectionId)? LastRequest { get; private set; }
        public Func<(string TenantId, string EnvironmentId, string ConnectionId), Task>? BeforeResolve { get; set; }

        public async Task<ConnectionAccessCredential> ResolveForUseAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default)
        {
            CallCount++;
            var request = (tenantId, environmentId, connectionId);
            LastRequest = request;
            if (BeforeResolve != null)
            {
                await BeforeResolve(request);
            }

            return new ConnectionAccessCredential($"access-{connectionId}", DateTimeOffset.UtcNow.AddHours(1));
        }
    }
}
