using System.Text.Json;
using CShells.Features;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Persistence.EFCore.Features;
using Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql.Extensions;
using Elsa.Connections.Credentials.Workflows.Features;
using Elsa.Connections.Features;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Secrets.Features;
using Elsa.Secrets.Persistence.EFCore.Features;
using Elsa.Secrets.Persistence.EFCore.PostgreSql.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.Persistence.EFCore.Features;
using Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql.Extensions;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Handlers;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdmissionPackageConsumer;

internal sealed class ConsumerHost(IHost host, Probe probe, HostLifetimes lifetimes) : IAsyncDisposable
{
    public IServiceProvider Services => host.Services;
    public Probe Probe => probe;
    public async Task StartAsync(CancellationToken cancellationToken) => await host.StartAsync(cancellationToken);
    public IDisposable EnterTenant() => Services.GetRequiredService<ITenantAccessor>().PushContext(new Tenant
    {
        Id = FixtureConstants.TenantId, Name = "Package consumer fixture"
    });

    public static ConsumerHost Create(string connectionString, string feature, bool admission, HostLifetimes lifetimes)
    {
        // No appsettings, command line/environment configuration providers or console logger.
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Logging.ClearProviders();
        var probe = new Probe(connectionString);
        builder.Logging.AddProvider(probe.Logs);
        var services = builder.Services;
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(probe);
        services.Configure<Elsa.Tenants.Options.TenantsOptions>(x => x.IsEnabled = true);
        services.AddSingleton<IConnectionCredentialProvider>(probe.Provider);
        services.AddScoped<INotificationHandler>(_ => probe);
        var module = services.CreateModule();
        // These packages currently expose classic infrastructure features, including in Shell hosts.
        module.Configure<ConnectionsFeature>();
        module.Configure<EFCoreConnectionsPersistenceFeature>(x =>
        {
            x.RunMigrations = false;
            x.UsePostgreSql(connectionString);
        });
        module.Configure<WorkflowCredentialBindingsFeature>(x => x.EnvironmentId = FixtureConstants.EnvironmentId);
        module.Configure<WorkflowCredentialUseGrantsFeature>();
        module.Configure<SecretsFeature>().ConfigureOptions = x => x.EncryptionKey = Enumerable.Range(1, 32).Select(n => (byte)n).ToArray();
        module.Configure<EFCoreSecretsPersistenceFeature>(x =>
        {
            x.RunMigrations = false;
            x.UsePostgreSql(connectionString);
        });
        if (feature == "shell")
        {
            module.Apply();
            ConfigureShell(services, connectionString, admission);
        }
        else
        {
            module.Configure<WorkflowRuntimeFeature>();
            module.Configure<WorkflowManagementFeature>();
            module.Configure<Elsa.Workflows.Features.WorkflowsFeature>(x => x.WithWorkflowExecutionPipeline(p => p.UseDefaultPipeline()));
            module.Configure<EFCoreWorkflowDefinitionPersistenceFeature>(x =>
            {
                x.RunMigrations = false;
                x.UsePostgreSql(connectionString);
            });
            module.Configure<EFCoreWorkflowInstancePersistenceFeature>(x =>
            {
                x.RunMigrations = false;
                x.UsePostgreSql(connectionString);
            });
            module.Configure<EFCoreWorkflowRuntimePersistenceFeature>(x =>
            {
                x.RunMigrations = false;
                x.UsePostgreSql(connectionString);
            });
            if (admission)
            {
                module.Configure<EFCoreAdmissionPersistenceFeature>(x =>
                {
                    x.TenantId = FixtureConstants.TenantId;
                    x.EnvironmentId = FixtureConstants.EnvironmentId;
                    x.RunMigrations = false;
                    x.UsePostgreSql(connectionString);
                });
                module.AddActivity<AdmissionActivity>();
            }
            else
            {
                module.AddWorkflow<GrantWorkflow>();
                module.AddActivity<GrantActivity>();
            }
            module.Apply();
        }
        if (admission)
        {
            var binding = CompileBinding();
            services.AddSingleton(binding);
            var configuration = new AdmissionHostConfiguration(FixtureConstants.TenantId, FixtureConstants.EnvironmentId,
                AuditedTypes(), [typeof(Workflow), typeof(AdmissionActivity)], [binding.Configuration]);
            if (feature == "shell")
            {
                new Elsa.Workflows.Admission.ShellFeatures.AdmissionFeature { Configuration = configuration }.ConfigureServices(services);
            }
            else
            {
                new Elsa.Workflows.Admission.Features.AdmissionFeature(module) { Configuration = configuration }.Apply();
            }
        }
        else
        {
            var policies = new FixturePolicies();
            services.AddSingleton<IConnectionUseAuthorizer>(policies);
            services.AddSingleton<IConnectionCredentialBindingManagementAuthorizer>(policies);
            services.AddSingleton<IConnectionCredentialGrantManagementAuthorizer>(policies);
            services.AddSingleton<IConnectionCredentialShareAuthorizer>(policies);
        }
        var host = builder.Build();
        try
        {
            lifetimes.CreatedHost();
            return new(host, probe, lifetimes);
        }
        catch
        {
            host.Dispose();
            throw;
        }
    }

    private static void ConfigureShell(IServiceCollection services, string connectionString, bool admission)
    {
        // The actual public Shell feature implementations, in declared dependency order.
        IShellFeature[] features =
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
            }
        ];
        foreach (var feature in features)
        {
            feature.ConfigureServices(services);
        }
        if (admission)
        {
            new Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql.ShellFeatures.PostgreSqlAdmissionPersistenceShellFeature
            {
                TenantId = FixtureConstants.TenantId, EnvironmentId = FixtureConstants.EnvironmentId,
                ConnectionString = connectionString, RunMigrations = false
            }.ConfigureServices(services);
            services.AddActivity<AdmissionActivity>();
        }
        else
        {
            services.AddWorkflow<GrantWorkflow>();
            services.AddActivity<GrantActivity>();
        }
    }

    private static AdmissionBinding CompileBinding()
    {
        // Metadata-only serializer compiler: no runtime/executor and no PostgreSQL connection.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var module = services.CreateModule();
        module.Configure<WorkflowManagementFeature>();
        module.Apply();
        using var compiler = services.BuildServiceProvider();
        var artifact = new WorkflowDefinition
        {
            Id = "package-admission-version", DefinitionId = "package-admission-definition", TenantId = FixtureConstants.TenantId,
            Version = 1, CreatedAt = FixtureConstants.ArtifactTime, IsLatest = false, IsPublished = false, MaterializerName = "Json",
            StringData = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["id"] = "package-admission-activity", ["type"] = ActivityTypeNameHelper.GenerateTypeName<AdmissionActivity>(), ["version"] = 1
            })
        };
        var fingerprint = AdmissionDefinitionFingerprint.Compute(artifact, compiler.GetRequiredService<IPayloadSerializer>());
        return new(artifact, new("package-subscription", FixtureConstants.TenantId, FixtureConstants.EnvironmentId,
            "package-logical-installation", "package-channel", artifact.DefinitionId, artifact.Id, artifact.Version, fingerprint,
            FixtureConstants.ArtifactTime.AddMinutes(-1), new(TimeSpan.FromHours(1), TimeSpan.FromDays(2), TimeSpan.FromDays(1),
                TimeSpan.FromMinutes(1), 4, 8, 4096, 256, AdmissionRejectedEventDisposition.Quarantine,
                AdmissionRejectedEventDisposition.Reject, "package-fixture-cleanup")));
    }

    private static IEnumerable<Type> AuditedTypes()
    {
        // Deliberate exact inventory of the selected built-in preparation and notification chain.
        // Unknown registrations are rejected by AdmissionHostConfiguration, never auto-allowlisted.
        Type[] direct =
        [
            typeof(WorkflowDefinitionService), typeof(WorkflowGraphBuilder), typeof(WorkflowInstanceManager), typeof(WorkflowStateExtractor),
            typeof(JsonWorkflowStateSerializer), typeof(JsonActivitySerializer), typeof(JsonPayloadSerializer), typeof(ActivityRegistry),
            typeof(ActivityRegistryLookupService), typeof(MaterializerRegistry), typeof(WorkflowLoggerStateGenerator),
            typeof(WorkflowCommitNotificationSender), typeof(ExecutionCycleAwareCommitStateHandler), typeof(BookmarksPersister),
            typeof(VariablePersistenceManager), typeof(NoopWorkflowCommitTransaction), typeof(WorkflowCommitNotificationBuffer),
            typeof(EvaluateParentInputProperties), typeof(ActivitySchedulerFactory), typeof(ExecutionCycleRegistry), typeof(StorageDriverManager),
            typeof(WorkflowInstanceStorageDriver), typeof(MemoryStorageDriver), typeof(ActivityInvoker), typeof(ActivityLoggerStateGenerator),
            typeof(TypedActivityProvider), typeof(WorkflowDefinitionActivityProvider), typeof(HostMethodActivityProvider), typeof(Probe)
        ];
        var management = new[] { "DeleteWorkflowInstances", "RefreshActivityRegistry", "UpdateConsumingWorkflows", "ValidateWorkflow", "ValidateOutputConverters" }
            .Select(name => typeof(WorkflowManagementFeature).Assembly.GetType("Elsa.Workflows.Management.Handlers.Notifications." + name, true)!);
        var runtime = new[]
        {
            "ResumeDispatchWorkflowActivity", "ResumeBulkDispatchWorkflowActivity", "ProcessWorkflowDispatchOutbox", "ResumeExecuteWorkflowActivity",
            "IndexTriggers", "CancelBackgroundActivities", "DeleteBookmarks", "DeleteTriggers", "DeleteActivityExecutionLogRecords",
            "DeleteWorkflowExecutionLogRecords", "RefreshActivityRegistry", "SignalBookmarkQueueWorker", "EvaluateParentLogPersistenceModes",
            "CaptureActivityExecutionState", "ValidateWorkflowRequestHandler"
        }.Select(name => typeof(WorkflowRuntimeFeature).Assembly.GetType("Elsa.Workflows.Runtime.Handlers." + name, true)!);
        return direct.Concat(management).Concat(runtime).Append(typeof(IStorageDriver).Assembly.GetType("Elsa.Workflows.Services.WorkflowStorageDriver", true)!);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await host.StopAsync(stop.Token);
        }
        finally
        {
            if (host is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
            else
            {
                host.Dispose();
            }
        }
        lifetimes.DisposedHost();
        Require.That(probe.Provider.Calls == 0, "provider-invoked");
        probe.Logs.AssertClean();
    }
}

internal sealed record AdmissionBinding(WorkflowDefinition Artifact, AdmissionSubscriptionConfiguration Configuration);
internal static class FixtureConstants
{
    public const string TenantId = "package-fixture-tenant";
    public const string EnvironmentId = "package-fixture-environment";
    public const string BindingId = "package-logical-binding";
    public const string SecretMarker = "synthetic-package-consumer-secret-never-export-8661";
    public static readonly DateTimeOffset ArtifactTime = new(2026, 10, 8, 0, 0, 0, TimeSpan.Zero);
}

internal sealed class HostLifetimes
{
    private static int _processActive;
    public int Created { get; private set; }
    public int Disposed { get; private set; }
    public int Active { get; private set; }
    public int MaximumConcurrent { get; private set; }

    public void CreatedHost()
    {
        if (Interlocked.CompareExchange(ref _processActive, 1, 0) != 0)
        {
            throw new ProofFailure("concurrent-execution-hosts");
        }
        Created++;
        Active++;
        MaximumConcurrent = Math.Max(MaximumConcurrent, Active);
    }

    public void DisposedHost()
    {
        Disposed++;
        Active--;
        Require.That(Interlocked.Exchange(ref _processActive, 0) == 1 && Active == 0, "host-disposal-order");
    }

    public CleanupEvidence Evidence(int expectedCreated)
    {
        Require.That(Created == expectedCreated && Disposed == Created && Active == 0 && MaximumConcurrent == 1, "host-lifetime-counts");
        return new(Disposed == Created && Active == 0, Created, MaximumConcurrent);
    }
}
