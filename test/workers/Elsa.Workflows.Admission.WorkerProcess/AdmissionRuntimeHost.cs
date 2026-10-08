using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Features;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Elsa.Workflows.Admission.Persistence.EFCore.Features;
using Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql.Extensions;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Activities.HostMethod;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Management.Providers;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Features;
using Elsa.Workflows.Runtime.Services;
using Elsa.Workflows.Serialization.Serializers;
using Elsa.Workflows.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Elsa.Workflows.Admission.WorkerProcess;

/// <summary>One actual guarded local execution host. Other worker commands are nonexecuting controllers.</summary>
public static class AdmissionRuntimeHost
{
    public static WorkflowDefinition Artifact() => new()
    {
        Id = "definition-version-fixed", DefinitionId = "definition-fixed", TenantId = AdmissionWorkerHost.TenantId,
        Version = 1, CreatedAt = AdmissionWorkerHost.Now, IsLatest = false, IsPublished = false,
        MaterializerName = "Json", StringData = JsonSerializer.Serialize(new
        {
            id = "admission-runtime-activity", type = ActivityTypeNameHelper.GenerateTypeName<AdmissionRuntimeActivity>(), version = 1
        })
    };

    public static AdmissionSubscriptionConfiguration Configuration()
    {
        // The fixed artifact contains only plain JSON values. The fingerprint is computed by
        // the normal serializer in CreateServices, without a second store-sharing host.
        return AdmissionWorkerHost.Configuration();
    }

    public static ServiceProvider CreateServices(string connectionString, AdmissionRuntimeProbe probe, bool shell = false,
        Action<IServiceCollection>? configure = null, IInterceptor? admissionInterceptor = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(probe);
        services.AddSingleton<IAdmissionExecutionObserver>(probe);
        services.AddScoped<INotificationHandler>(_ => probe);
        var module = services.CreateModule();
        module.Configure<ConnectionsFeature>();
        var instanceWrites = new AdmissionObservedInstanceWrites(probe);
        if (shell)
        {
            // Connections currently exposes classic registration. Only that existing credential/
            // grant infrastructure is applied through IModule; ALL workflow and persistence
            // registrations below use the actual ShellFeatures implementations.
            module.Apply();
            ConfigureShellServices(services, connectionString, instanceWrites, admissionInterceptor);
        }
        else
        {
            module.Configure<WorkflowRuntimeFeature>();
            module.Configure<Elsa.Workflows.Features.WorkflowsFeature>(feature => feature.WithWorkflowExecutionPipeline(pipeline => pipeline.UseDefaultPipeline()));
            module.Configure<WorkflowManagementFeature>();
            module.AddActivity<AdmissionRuntimeActivity>();
            module.Configure<EFCoreAdmissionPersistenceFeature>(feature =>
            {
                feature.TenantId = AdmissionWorkerHost.TenantId;
                feature.EnvironmentId = AdmissionWorkerHost.EnvironmentId;
                feature.RunMigrations = false;
                feature.UsePostgreSql(connectionString);
                if (admissionInterceptor != null)
                {
                    var inner = feature.DbContextOptionsBuilder;
                    feature.DbContextOptionsBuilder = (provider, builder) =>
                    {
                        inner(provider, builder);
                        builder.AddInterceptors(admissionInterceptor);
                    };
                }
            });
            module.Configure<EFCoreWorkflowDefinitionPersistenceFeature>(feature => feature.UsePostgreSql(connectionString));
            module.Configure<EFCoreWorkflowInstancePersistenceFeature>(feature =>
            {
                feature.UsePostgreSql(connectionString);
                var inner = feature.DbContextOptionsBuilder;
                feature.DbContextOptionsBuilder = (provider, builder) =>
                {
                    inner(provider, builder);
                    builder.AddInterceptors(instanceWrites);
                };
            });
            module.Configure<EFCoreWorkflowRuntimePersistenceFeature>(feature => feature.UsePostgreSql(connectionString));
            module.Apply();
        }
        services.Replace(ServiceDescriptor.Singleton<ISystemClock>(new AdmissionRuntimeClock()));
        services.Decorate<IWorkflowStateExtractor, AdmissionObservedStateExtractor>();
        services.Decorate<ICommitStateHandler, AdmissionObservedCommit>();
        services.Decorate<IBookmarkStore, AdmissionObservedBookmarkStore>();
        // A serializer can be instantiated against the SAME final service container after
        // registration. The deferred descriptor constructs the immutable allowlist before use.
        services.AddSingleton(provider =>
        {
            var artifact = Artifact();
            var configuration = Configuration() with
            {
                DefinitionFingerprint = AdmissionDefinitionFingerprint.Compute(artifact, provider.GetRequiredService<IPayloadSerializer>())
            };
            return new AdmissionRuntimeBinding(artifact, configuration);
        });
        // Build-time configuration uses the same fixed payload serializer representation as
        // JsonPayloadSerializer: it is generated below by a metadata-only compiler whose
        // stores are memory-only and never share the PostgreSQL execution store.
        var binding = CompileBinding();
        var host = new AdmissionHostConfiguration(AdmissionWorkerHost.TenantId, AdmissionWorkerHost.EnvironmentId,
            ReviewedTypes(), [typeof(Workflow), typeof(AdmissionRuntimeActivity)], [binding.Configuration]);
        if (shell)
        {
            new Elsa.Workflows.Admission.ShellFeatures.AdmissionFeature { Configuration = host }.ConfigureServices(services);
        }
        else
        {
            var feature = new Elsa.Workflows.Admission.Features.AdmissionFeature(module) { Configuration = host };
            feature.Apply();
        }
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private static void ConfigureShellServices(IServiceCollection services, string connectionString,
        AdmissionObservedInstanceWrites instanceWrites, IInterceptor? admissionInterceptor)
    {
        // Explicit dependency order from the selected ShellFeature attributes. This invokes
        // the real features, not a classic registration wearing an Admission Shell wrapper.
        CShells.Features.IShellFeature[] features =
        [
            new Elsa.Common.ShellFeatures.SystemClockFeature(),
            new Elsa.Expressions.ShellFeatures.ExpressionsFeature(),
            new Elsa.Common.ShellFeatures.MultitenancyFeature(),
            new Elsa.Common.ShellFeatures.MediatorFeature(),
            new Elsa.Common.ShellFeatures.DefaultFormattersFeature(),
            new Elsa.Workflows.ShellFeatures.CommitStrategiesFeature(),
            new Elsa.Common.ShellFeatures.StringCompressionFeature(),
            new Elsa.Caching.ShellFeatures.MemoryCacheFeature(),
            new Elsa.Workflows.ShellFeatures.WorkflowsFeature(),
            new Elsa.Workflows.Management.ShellFeatures.WorkflowDefinitionsFeature(),
            new Elsa.Workflows.Management.ShellFeatures.WorkflowInstancesFeature(),
            new Elsa.Workflows.Management.ShellFeatures.WorkflowManagementFeature(),
            new Elsa.Workflows.Runtime.ShellFeatures.WorkflowRuntimeFeature(),
            new Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Management.PostgreSqlWorkflowDefinitionPersistenceFeature
            {
                ConnectionString = connectionString, RunMigrations = false
            },
            new Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Management.PostgreSqlWorkflowInstancePersistenceFeature
            {
                ConnectionString = connectionString, RunMigrations = false
            },
            new Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Runtime.PostgreSqlWorkflowRuntimePersistenceFeature
            {
                ConnectionString = connectionString, RunMigrations = false
            },
            new Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql.ShellFeatures.PostgreSqlAdmissionPersistenceShellFeature
            {
                TenantId = AdmissionWorkerHost.TenantId, EnvironmentId = AdmissionWorkerHost.EnvironmentId,
                ConnectionString = connectionString, RunMigrations = false
            }
        ];
        foreach (var feature in features)
        {
            feature.ConfigureServices(services);
        }
        services.AddActivity<AdmissionRuntimeActivity>();
        // AddDbContextFactory retains the real selected-provider factory. Subsequent options
        // registrations attach only fixture interceptors to that same actual context.
        services.AddDbContext<ManagementElsaDbContext>((_, builder) => builder.AddInterceptors(instanceWrites));
        if (admissionInterceptor != null)
        {
            services.AddDbContext<AdmissionElsaDbContext>((_, builder) => builder.AddInterceptors(admissionInterceptor));
        }
    }

    private static AdmissionRuntimeBinding CompileBinding()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var module = services.CreateModule();
        module.Configure<WorkflowManagementFeature>();
        module.Apply();
        using var compiler = services.BuildServiceProvider();
        var artifact = Artifact();
        return new(artifact, Configuration() with
        {
            DefinitionFingerprint = AdmissionDefinitionFingerprint.Compute(artifact, compiler.GetRequiredService<IPayloadSerializer>())
        });
    }

    public static IDisposable EnterTenant(IServiceProvider services) => services.GetRequiredService<ITenantAccessor>().PushContext(new Tenant
    {
        Id = AdmissionWorkerHost.TenantId, Name = "Admission fixture"
    });

    public static async Task MigrateAsync(IServiceProvider services)
    {
        await AdmissionWorkerHost.MigrateAsync(services);
        await using var management = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();
        await management.Database.MigrateAsync();
        await using var runtime = await services.GetRequiredService<IDbContextFactory<RuntimeElsaDbContext>>().CreateDbContextAsync();
        await runtime.Database.MigrateAsync();
        await services.GetRequiredService<AdmissionRuntimeProbe>().InitializeAsync();
    }

    public static async Task<AdmissionSubscription> BootstrapAsync(IServiceProvider services, bool activate = true)
    {
        await services.GetRequiredService<AdmissionHostConfiguration>().ValidateAsync(services);
        await services.PopulateRegistriesAsync();
        var binding = services.GetRequiredService<AdmissionRuntimeBinding>();
        var bootstrap = services.GetRequiredService<AdmissionBootstrapService>();
        var subscription = await bootstrap.ProvisionAsync(binding.Configuration, binding.Artifact);
        return activate ? (await bootstrap.ActivateAsync(subscription.Id, subscription.Revision))! : subscription;
    }

    private static IEnumerable<Type> ReviewedTypes()
    {
        var direct = new[]
        {
            typeof(WorkflowDefinitionService), typeof(WorkflowGraphBuilder), typeof(WorkflowInstanceManager), typeof(WorkflowStateExtractor),
            typeof(JsonWorkflowStateSerializer), typeof(JsonActivitySerializer), typeof(JsonPayloadSerializer), typeof(ActivityRegistry),
            typeof(ActivityRegistryLookupService), typeof(MaterializerRegistry), typeof(WorkflowLoggerStateGenerator),
            typeof(WorkflowCommitNotificationSender), typeof(ExecutionCycleAwareCommitStateHandler), typeof(BookmarksPersister),
            typeof(VariablePersistenceManager), typeof(NoopWorkflowCommitTransaction), typeof(WorkflowCommitNotificationBuffer),
            typeof(ActivitySchedulerFactory), typeof(ActivityInvoker), typeof(ActivityLoggerStateGenerator), typeof(TypedActivityProvider), typeof(WorkflowDefinitionActivityProvider), typeof(HostMethodActivityProvider),
            typeof(AdmissionRuntimeProbe), typeof(AdmissionObservedStateExtractor), typeof(AdmissionObservedCommit), typeof(AdmissionObservedBookmarkStore)
        };
        var management = new[] { "DeleteWorkflowInstances", "RefreshActivityRegistry", "UpdateConsumingWorkflows", "ValidateWorkflow", "ValidateOutputConverters" }
            .Select(name => typeof(WorkflowManagementFeature).Assembly.GetType("Elsa.Workflows.Management.Handlers.Notifications." + name, true)!);
        var runtime = new[]
        {
            "ResumeDispatchWorkflowActivity", "ResumeBulkDispatchWorkflowActivity", "ProcessWorkflowDispatchOutbox", "ResumeExecuteWorkflowActivity",
            "IndexTriggers", "CancelBackgroundActivities", "DeleteBookmarks", "DeleteTriggers", "DeleteActivityExecutionLogRecords",
            "DeleteWorkflowExecutionLogRecords", "RefreshActivityRegistry", "SignalBookmarkQueueWorker", "EvaluateParentLogPersistenceModes",
            "CaptureActivityExecutionState", "ValidateWorkflowRequestHandler"
        }.Select(name => typeof(WorkflowRuntimeFeature).Assembly.GetType("Elsa.Workflows.Runtime.Handlers." + name, true)!);
        return direct.Concat(management).Concat(runtime);
    }
}

public sealed record AdmissionRuntimeBinding(WorkflowDefinition Artifact, AdmissionSubscriptionConfiguration Configuration);
internal sealed class AdmissionRuntimeClock : ISystemClock
{
    public DateTimeOffset UtcNow => AdmissionWorkerHost.Now;
}
