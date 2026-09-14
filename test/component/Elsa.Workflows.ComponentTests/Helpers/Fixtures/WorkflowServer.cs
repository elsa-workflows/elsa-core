using System.Collections.Concurrent;
using System.Reflection;
using Elsa.Alterations.Extensions;
using Elsa.Caching;
using Elsa.Caching.Options;
using Elsa.Common.Features;
using Elsa.Common.RecurringTasks;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Identity.Providers;
using Elsa.Http.Options;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using Elsa.Persistence.EFCore.Modules.Identity;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Testing.Shared.Handlers;
using Elsa.Testing.Shared.Services;
using Elsa.Tenants;
using Elsa.Tenants.AspNetCore;
using Elsa.Tenants.Extensions;
using Elsa.Workflows.ComponentTests.Host;
using Elsa.Workflows.ComponentTests.Decorators;
using Elsa.Workflows.ComponentTests.Materializers;
using Elsa.Workflows.ComponentTests.Scenarios.DistributedLockResilience.Mocks;
using Elsa.Workflows.ComponentTests.Scenarios.HostMethodActivities;
using Elsa.Workflows.ComponentTests.Scenarios.OutputConverters;
using Elsa.Workflows.ComponentTests.Services;
using Elsa.Workflows.ComponentTests.WorkflowProviders;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Management;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Distributed.Extensions;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Tasks;
using FluentStorage;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refit;
using TUnit.AspNetCore;
using static Elsa.Api.Client.RefitSettingsHelper;

namespace Elsa.Workflows.ComponentTests.Fixtures;

/// <summary>
/// Session-owned root factory. TUnit creates a derived TestServer host for each pod by
/// executing the dedicated component-test entry point.
/// </summary>
public sealed class ComponentTestWebApplicationFactory : TestWebApplicationFactory<ComponentTestHost>
{
    private const string TestSigningKey = "development-only-secret-signing-key-change-before-production";
    private readonly ConcurrentBag<ElsaModuleRegistryLease> _moduleRegistryLeases = [];
    private int _baseFactoryDisposed;

    internal bool BaseFactoryDisposed => Volatile.Read(ref _baseFactoryDisposed) != 0;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
    }

    internal void ConfigureComponentHost(IWebHostBuilder builder, ComponentTestHostOptions hostOptions)
    {
        var moduleRegistryLease = new ElsaModuleRegistryLease();
        _moduleRegistryLeases.Add(moduleRegistryLease);

        builder
            .UseEnvironment(Environments.Development)
            .ConfigureServices(services =>
            {
                moduleRegistryLease.Bind(services);
                var preExistingHostedServices = hostOptions.DatabaseBootstrapOnly
                    ? services.Where(x => x.ServiceType == typeof(IHostedService)).ToArray()
                    : [];

                try
                {
                    services.AddSingleton(hostOptions);
                    var provisioningDescriptor = ServiceDescriptor.Singleton<IHostedService>(
                        new ComponentCatalogProvisioningService(hostOptions.Catalog));
                    services.Insert(0, provisioningDescriptor);
                    ServiceDescriptor? migrationDescriptor = null;
                    if (hostOptions.MigrateDatabase)
                    {
                        migrationDescriptor = ServiceDescriptor.Singleton<IHostedService, ComponentDatabaseMigrationService>();
                        services.Insert(1, migrationDescriptor);
                    }
                    services.Configure<HttpFileCacheOptions>(options =>
                        options.LocalCacheDirectory = hostOptions.HttpFileCacheDirectory);

                    var module = services.ConfigureElsa(ConfigureElsaForTesting);
                    var registryCleanupDescriptor = moduleRegistryLease.CaptureAndRegister(module);
                    module.Apply();

                    // These component-host values must be registered after Apply because runtime
                    // features add their own defaults while the module is being applied.
                    services.Configure<RecurringTaskOptions>(options =>
                    {
                        options.Schedule.ConfigureTask<TriggerBookmarkQueueRecurringTask>(TimeSpan.FromSeconds(300));
                        options.Schedule.ConfigureTask<PurgeBookmarkQueueRecurringTask>(TimeSpan.FromSeconds(300));
                        options.Schedule.ConfigureTask<RestartInterruptedWorkflowsTask>(TimeSpan.FromSeconds(15));
                    });
                    services.Configure<RuntimeOptions>(options => options.InactivityThreshold = TimeSpan.FromSeconds(15));
                    services.Configure<BookmarkQueuePurgeOptions>(options => options.Ttl = TimeSpan.FromSeconds(3600));
                    services.Configure<CachingOptions>(options => options.CacheDuration = TimeSpan.FromDays(1));
                    services.Configure<IncidentOptions>(options => options.DefaultIncidentStrategy = typeof(ContinueWithIncidentsStrategy));

                    services.Decorate<IDistributedLockProvider, SelectiveMockLockProvider>();
                    services.AddSingleton(serviceProvider =>
                    {
                        var provider = serviceProvider.GetRequiredService<IDistributedLockProvider>();
                        if (provider is not SelectiveMockLockProvider selectiveProvider)
                            throw new InvalidOperationException($"Expected {nameof(SelectiveMockLockProvider)}, but got {provider.GetType().Name}.");
                        return selectiveProvider;
                    });

                    services
                        .AddOutputConverter<TestOutputConverter>(TestOutputConverter.Descriptor)
                        .AddSingleton<SignalManager>()
                        .AddScoped<AsyncWorkflowRunner>()
                        .AddSingleton<WorkflowEvents>()
                        .AddScoped<WorkflowDefinitionEvents>()
                        .AddSingleton<TriggerChangeTokenSignalEvents>()
                        .AddScoped<IWorkflowMaterializer, TestWorkflowMaterializer>()
                        .AddNotificationHandlersFrom<WorkflowServer>()
                        .AddWorkflowsProvider<TestWorkflowProvider>()
                        .AddNotificationHandlersFrom<WorkflowEventHandlers>()
                        .Decorate<IChangeTokenSignaler, EventPublishingChangeTokenSignaler>()
                        .AddSingleton<TestBookmarkQueueWorker>()
                        .AddSingleton<TestJavaScriptState>()
                        .AddSingleton<TestHostMethodState>()
                        .AddSingleton(serviceProvider =>
                            serviceProvider.GetRequiredService<ComponentTestHostOptions>().WorkflowExecutionTracker)
                        .Decorate<IWorkflowRunner, TrackingWorkflowRunner>();

                    if (hostOptions.DatabaseBootstrapOnly)
                    {
                        if (migrationDescriptor is null)
                            throw new InvalidOperationException("A database-bootstrap host must enable the component migration service.");

                        var hostedServicesToKeep = preExistingHostedServices
                            .Append(provisioningDescriptor)
                            .Append(migrationDescriptor)
                            .Append(registryCleanupDescriptor)
                            .ToArray();

                        for (var i = services.Count - 1; i >= 0; i--)
                        {
                            var descriptor = services[i];
                            if (descriptor.ServiceType == typeof(IHostedService) &&
                                !hostedServicesToKeep.Any(x => ReferenceEquals(x, descriptor)))
                                services.RemoveAt(i);
                        }
                    }
                }
                catch (Exception configurationFailure)
                {
                    try
                    {
                        moduleRegistryLease.ReleaseBeforeHostStarted(services);
                    }
                    catch (Exception cleanupFailure)
                    {
                        throw new AggregateException(
                            "Component host configuration and Elsa module-registry cleanup both failed.",
                            configurationFailure,
                            cleanupFailure);
                    }

                    throw;
                }
            });
    }

    private static void ConfigureElsaForTesting(IModule elsa)
    {
        elsa
            .AddWorkflowsFrom<WorkflowServer>()
            .AddActivitiesFrom<WorkflowServer>()
            .AddActivityHost<TestHostMethod>()
            .UseDefaultAuthentication(authentication => authentication.UseDevelopmentAdminApiKey())
            .UseIdentity(identity =>
            {
                identity.TokenOptions += options => options.SigningKey = TestSigningKey;
                identity.UseEntityFrameworkCore(ef =>
                {
                    ef.UseSqlServer(GetConnectionString);
                    ef.RunMigrations = false;
                });
            })
            .UseWorkflowManagement(management =>
            {
                management.UseEntityFrameworkCore(ef =>
                {
                    ef.UseSqlServer(GetConnectionString);
                    ef.RunMigrations = false;
                });
            })
            .UseWorkflowRuntime(runtime =>
            {
                runtime.UseEntityFrameworkCore(ef =>
                {
                    ef.UseSqlServer(GetConnectionString);
                    ef.RunMigrations = false;
                });
                runtime.DistributedLockProvider = serviceProvider =>
                {
                    var options = serviceProvider.GetRequiredService<ComponentTestHostOptions>();
                    return new FileDistributedSynchronizationProvider(new DirectoryInfo(options.LockDirectory));
                };
                runtime.DistributedLockingOptions = options => options.AllowLocalLockProviderInDistributedRuntime = true;
                runtime.BookmarkQueueWorker = serviceProvider => serviceProvider.GetRequiredService<TestBookmarkQueueWorker>();
            })
            .UseFluentStorageProvider(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<ComponentTestHostOptions>();
                return StorageFactory.Blobs.DirectoryFiles(options.ScenarioDirectory);
            })
            .UseJavaScript(options =>
            {
                options.AllowClrAccess = true;
                options.ConfigureEngine((engine, context) =>
                {
                    var state = context.ServiceProvider.GetRequiredService<TestJavaScriptState>();
                    engine.SetValue("getStaticValue", (Func<object?>)(() => state.Value));
                });
            })
            .UseAlterations(alterations => alterations.UseEntityFrameworkCore(ef =>
            {
                ef.UseSqlServer(GetConnectionString);
                ef.RunMigrations = false;
            }));

        elsa.Configure<MultitenancyFeature>(feature =>
            feature.UseTenantsProvider(_ => new TestTenantsProvider(string.Empty, "Tenant1", "Tenant2", "Tenant3")));
        elsa.UseTenants(tenants => tenants.ConfigureMultitenancy(options =>
            options.TenantResolverPipelineBuilder = new TenantResolverPipelineBuilder()
                .Append<ComponentTestTenantResolver>()));
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Authorization = new("ApiKey", AdminApiKeyProvider.DevelopmentApiKey);
    }

    public override async ValueTask DisposeAsync()
    {
        List<Exception>? failures = null;
        var factoryDisposed = false;
        try
        {
            await base.DisposeAsync();
            factoryDisposed = true;
            Volatile.Write(ref _baseFactoryDisposed, 1);
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }

        foreach (var lease in _moduleRegistryLeases)
        {
            try
            {
                // A successful root-factory teardown has disposed every retained derived
                // factory. It is now safe to exact-remove a lease even when host startup
                // failed before the normal stopped-lifecycle callback could run.
                lease.ReleaseAfterFactoryDisposed(factoryDisposed);
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }

        if (failures is { Count: > 0 })
            throw new AggregateException("Failed to release one or more Elsa module-registry leases.", failures);
    }

    private static string GetConnectionString(IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<ComponentTestHostOptions>().ConnectionString;
}

/// <summary>
/// Immutable resources shared by every pod in the component-test session.
/// </summary>
internal sealed record ComponentTestHostOptions(
    string ConnectionString,
    string ScenarioDirectory,
    string LockDirectory,
    string HttpFileCacheDirectory,
    WorkflowExecutionTracker WorkflowExecutionTracker,
    ComponentTestCatalog Catalog,
    bool MigrateDatabase,
    bool DatabaseBootstrapOnly,
    ComponentDatabaseBootstrapTracker? DatabaseBootstrapTracker);

internal sealed class ComponentDatabaseBootstrapTracker
{
    private readonly ConcurrentDictionary<string, byte> _migrations = new(StringComparer.Ordinal);

    public int MigrationCount => _migrations.Count;

    public void RecordMigration(Type dbContextType)
    {
        if (!_migrations.TryAdd(dbContextType.Name, 0))
            throw new InvalidOperationException($"The template migrated {dbContextType.Name} more than once.");
    }
}

/// <summary>
/// Creates the shared app catalog before Elsa's tenant activation and migration hosted services run.
/// The host itself is materialized while TUnit.AspNetCore holds its native server-init gate.
/// </summary>
internal sealed class ComponentCatalogProvisioningService(ComponentTestCatalog catalog) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => catalog.ProvisionAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// Applies each distinct component schema once to the session template. The restored app catalog
/// starts with those schemas, while Elsa's per-tenant migration startup tasks remain disabled.
/// </summary>
internal sealed class ComponentDatabaseMigrationService(
    IServiceScopeFactory scopeFactory,
    ComponentTestHostOptions hostOptions) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) =>
        hostOptions.Catalog.MigrateAsync(MigrateCoreAsync, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task MigrateCoreAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;

        await MigrateAsync<IdentityElsaDbContext>(services, cancellationToken);
        await MigrateAsync<ManagementElsaDbContext>(services, cancellationToken);
        await MigrateAsync<RuntimeElsaDbContext>(services, cancellationToken);
        await MigrateAsync<AlterationsElsaDbContext>(services, cancellationToken);
    }

    private async Task MigrateAsync<TDbContext>(IServiceProvider services, CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        var factory = services.GetRequiredService<IDbContextFactory<TDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
        hostOptions.DatabaseBootstrapTracker?.RecordMigration(typeof(TDbContext));
    }
}

/// <summary>
/// Owns one derived TestServer host and every client created from it.
/// </summary>
public sealed class WorkflowServer : IAsyncDisposable
{
    private readonly object _clientLock = new();
    private readonly List<HttpClient> _clients = [];
    private readonly TracedWebApplicationFactory<ComponentTestHost> _factory;
    private readonly ComponentPodHarness? _ownedPod;
    private Task? _disposeTask;
    private bool _disposed;

    private WorkflowServer(
        ComponentPodHarness ownedPod,
        TracedWebApplicationFactory<ComponentTestHost> factory)
    {
        _ownedPod = ownedPod;
        _factory = factory;
    }

    public IServiceProvider Services => _factory.Services;

    internal static async Task<WorkflowServer> CreateAsync(
        ComponentTestWebApplicationFactory rootFactory,
        ComponentTestHostOptions hostOptions,
        TestContext testContext)
    {
        var pod = new ComponentPodHarness(rootFactory, hostOptions);
        try
        {
            await pod.StartAsync(testContext);
            return new WorkflowServer(pod, pod.Factory);
        }
        catch (Exception initializationFailure)
        {
            try
            {
                await pod.DisposeAsync();
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException("Component pod initialization and cleanup both failed.", initializationFailure, cleanupFailure);
            }

            throw;
        }
    }

    public TClient CreateApiClient<TClient>()
    {
        var client = CreateTrackedClient();
        client.BaseAddress = new Uri(client.BaseAddress!, "/elsa/api");
        client.Timeout = TimeSpan.FromMinutes(1);
        return RestService.For<TClient>(client, CreateRefitSettings(Services));
    }

    public HttpClient CreateHttpClient()
    {
        var client = CreateTrackedClient();
        client.BaseAddress = new Uri(client.BaseAddress!, "/elsa/api/");
        client.Timeout = TimeSpan.FromMinutes(1);
        return client;
    }

    public HttpClient CreateHttpWorkflowClient()
    {
        var client = CreateTrackedClient();
        client.BaseAddress = new Uri(client.BaseAddress!, "/workflows/");
        client.Timeout = TimeSpan.FromMinutes(1);
        return client;
    }

    internal void DisposeTrackedClients()
    {
        HttpClient[] clients;
        lock (_clientLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            clients = [.. _clients];
            _clients.Clear();
        }

        foreach (var client in clients)
            client.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        HttpClient[] clients;

        lock (_clientLock)
        {
            if (_disposeTask is not null)
                return new ValueTask(_disposeTask);

            _disposed = true;
            clients = [.. _clients];
            _clients.Clear();
            _disposeTask = DisposeCoreAsync(clients);
            return new ValueTask(_disposeTask);
        }
    }

    private async Task DisposeCoreAsync(HttpClient[] clients)
    {
        List<Exception>? failures = null;

        foreach (var client in clients)
        {
            try
            {
                client.Dispose();
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }

        if (_ownedPod is not null)
        {
            try
            {
                await _ownedPod.DisposeAsync();
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }

        if (failures is { Count: > 0 })
            throw new AggregateException("Failed to dispose a component-test workflow server.", failures);
    }

    private HttpClient CreateTrackedClient()
    {
        lock (_clientLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var client = _factory.CreateClient();
            _clients.Add(client);
            return client;
        }
    }
}

/// <summary>
/// Routes lazy clustered-pod creation through the same native TUnit.AspNetCore server-init gate as Pod 1.
/// </summary>
internal sealed class ComponentPodHarness : WebApplicationTest<ComponentTestWebApplicationFactory, ComponentTestHost>, IAsyncDisposable
{
    private readonly ComponentTestWebApplicationFactory _rootFactory;
    private readonly ComponentTestHostOptions _hostOptions;
    private int _disposed;

    public ComponentPodHarness(
        ComponentTestWebApplicationFactory rootFactory,
        ComponentTestHostOptions hostOptions)
    {
        _rootFactory = rootFactory;
        _hostOptions = hostOptions;
    }

    public Task StartAsync(TestContext testContext) => InitializeFactoryAsync(testContext);

    protected override Task SetupAsync()
    {
        GlobalFactory = _rootFactory;
        return Task.CompletedTask;
    }

    protected override void ConfigureTestOptions(WebApplicationTestOptions options)
    {
        options.AutoConfigureOpenTelemetry = false;
        options.AutoPropagateHttpClientFactory = false;
    }

    protected override void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        _rootFactory.ConfigureComponentHost(builder, _hostOptions);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        await DisposeFactoryAsync();
    }
}

/// <summary>
/// Removes only one disposed host's exact service-collection key from Elsa's process-wide module registry.
/// </summary>
internal sealed class ElsaModuleRegistryLease
{
    private readonly object _captureLock = new();
    private IServiceCollection? _services;
    private IModule? _module;
    private Exception? _lifecycleReleaseFailure;
    private int _hostStopped;
    private int _releaseState;

    public void Bind(IServiceCollection services)
    {
        lock (_captureLock)
        {
            if (_services is not null)
                throw new InvalidOperationException("A module-registry lease cannot bind more than one service collection.");

            _services = services;
        }
    }

    public ServiceDescriptor CaptureAndRegister(IModule module)
    {
        IServiceCollection services;
        lock (_captureLock)
        {
            services = _services
                ?? throw new InvalidOperationException("A module-registry lease must bind its service collection before capturing a module.");
            if (_module is not null)
                throw new InvalidOperationException("A module-registry lease cannot capture more than one module.");
            if (!ReferenceEquals(module.Services, services))
                throw new InvalidOperationException("Elsa returned a module for a different service collection.");

            var modules = GetModules();
            if (!modules.TryGetValue(services, out var registeredModule) || !ReferenceEquals(registeredModule, module))
                throw new InvalidOperationException("The host's exact service collection and module pair was not registered with Elsa.");

            _module = module;
        }

        var descriptor = ServiceDescriptor.Singleton<IHostedService>(serviceProvider =>
            new ElsaModuleRegistryCleanupService(
                this,
                serviceProvider.GetRequiredService<IHostApplicationLifetime>()));
        services.Insert(0, descriptor);
        return descriptor;
    }

    public void ReleaseAfterHostStopped()
    {
        Volatile.Write(ref _hostStopped, 1);
        try
        {
            Release(requireStoppedHost: true);
        }
        catch (Exception exception)
        {
            Interlocked.CompareExchange(ref _lifecycleReleaseFailure, exception, null);
        }
    }

    public void ReleaseBeforeHostStarted(IServiceCollection services)
    {
        var boundServices = Volatile.Read(ref _services);
        if (boundServices is not null && !ReferenceEquals(boundServices, services))
            throw new InvalidOperationException("A module-registry lease cannot release a different service collection.");

        Release(requireStoppedHost: false);
    }

    public void ReleaseAfterFactoryDisposed(bool factoryDisposed)
    {
        var hadCapturedModule = Volatile.Read(ref _module) is not null;
        var hostStopped = Volatile.Read(ref _hostStopped) != 0;
        Exception? fallbackFailure = null;
        try
        {
            // A failed root teardown may have aborted before disposing later retained
            // children. Preserve those live hosts' entries; exact fallback detachment is
            // safe only after successful teardown or positive ApplicationStopped evidence.
            Release(requireStoppedHost: !factoryDisposed);
        }
        catch (Exception exception)
        {
            fallbackFailure = exception;
        }

        var lifecycleFailure = Interlocked.Exchange(ref _lifecycleReleaseFailure, null);
        if (fallbackFailure is not null && lifecycleFailure is not null)
            throw new AggregateException("Both lifecycle and factory-fallback module-registry cleanup failed.", lifecycleFailure, fallbackFailure);
        if (fallbackFailure is not null)
            throw new InvalidOperationException("Factory-fallback module-registry cleanup failed.", fallbackFailure);
        if (lifecycleFailure is not null)
            throw new InvalidOperationException("Lifecycle module-registry cleanup failed before the factory fallback succeeded.", lifecycleFailure);
        if (factoryDisposed && hadCapturedModule && !hostStopped)
            throw new InvalidOperationException("The host never reached ApplicationStopped; its exact Elsa module-registry entry was detached by the factory fallback.");
    }

    private void Release(bool requireStoppedHost = true)
    {
        var priorState = Interlocked.CompareExchange(ref _releaseState, 1, 0);
        if (priorState == 2)
            return;
        if (priorState == 1)
            throw new InvalidOperationException("Concurrent Elsa module-registry release is not supported.");

        try
        {
            var services = Volatile.Read(ref _services);
            if (services is null)
            {
                Volatile.Write(ref _releaseState, 2);
                return;
            }

            var module = Volatile.Read(ref _module);
            if (module is null)
            {
                var registry = GetModules();
                if (!registry.TryGetValue(services, out module))
                {
                    Volatile.Write(ref _services, null);
                    Volatile.Write(ref _releaseState, 2);
                    return;
                }

                if (!ReferenceEquals(module.Services, services))
                    throw new InvalidOperationException("Elsa's module registry returned a module for a different service collection.");
                Volatile.Write(ref _module, module);
            }

            if (requireStoppedHost && Volatile.Read(ref _hostStopped) == 0)
                throw new InvalidOperationException("The component host did not reach its stopped lifecycle phase; its Elsa module-registry entry remains attached.");

            var modules = GetModules();
            if (!modules.TryRemove(new KeyValuePair<IServiceCollection, IModule>(services, module)))
            {
                if (modules.TryGetValue(services, out var registeredModule))
                    throw new InvalidOperationException(ReferenceEquals(registeredModule, module)
                        ? "Failed to remove the stopped host's exact Elsa module-registry entry."
                        : "The stopped host's Elsa module-registry entry was replaced unexpectedly.");
            }

            if (modules.ContainsKey(services))
                throw new InvalidOperationException("The stopped host's Elsa module-registry entry is still present.");

            Volatile.Write(ref _services, null);
            Volatile.Write(ref _module, null);
            Volatile.Write(ref _releaseState, 2);
        }
        catch
        {
            Volatile.Write(ref _releaseState, 0);
            throw;
        }
    }

    private static ConcurrentDictionary<IServiceCollection, IModule> GetModules()
    {
        const string fieldName = "Modules";
        var extensionsType = typeof(Elsa.Features.ElsaFeature).Assembly.GetType("Elsa.Extensions.ModuleExtensions", throwOnError: true)!;
        var field = extensionsType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            ?? throw new MissingFieldException(extensionsType.FullName, fieldName);
        var expectedType = typeof(IDictionary<IServiceCollection, IModule>);
        if (field.FieldType != expectedType || !field.IsStatic || !field.IsPrivate || !field.IsInitOnly)
            throw new InvalidOperationException($"Unexpected Elsa module registry type '{field.FieldType}'.");
        if (field.GetValue(null) is not ConcurrentDictionary<IServiceCollection, IModule> modules)
            throw new InvalidOperationException("Elsa's module registry is unavailable.");
        return modules;
    }
}

/// <summary>
/// Releases a host's exact Elsa module-registry key from ApplicationStopped, after every
/// hosted-service stop phase has completed but before the service provider is disposed.
/// </summary>
internal sealed class ElsaModuleRegistryCleanupService(
    ElsaModuleRegistryLease lease,
    IHostApplicationLifetime applicationLifetime) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = applicationLifetime.ApplicationStopped.Register(lease.ReleaseAfterHostStopped);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
