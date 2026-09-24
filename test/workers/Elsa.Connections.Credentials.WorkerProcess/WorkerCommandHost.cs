using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Connections.Credentials.Persistence.EFCore.Features;
using Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql.Extensions;
using Elsa.Connections.Credentials.Workflows.Contracts;
using Elsa.Connections.Credentials.Workflows.Features;
using Elsa.Connections.Features;
using Elsa.Connections.Models;
using Elsa.Connections.Services;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Features;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore.PostgreSql.Extensions;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Connections.Credentials.WorkerProcess;

public static class WorkerCommandHost
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            await using var serviceProvider = CreateServiceProvider();
            var command = args.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(command))
            {
                return WriteError("command_required");
            }

            if (command == "migrate")
            {
                await MigrateAsync(serviceProvider);
                return WriteResult(new { migrated = true });
            }

            var settings = WorkerSettings.FromEnvironment();
            using var tenant = command == "restart-workflow"
                ? null
                : serviceProvider.GetRequiredService<DefaultTenantAccessor>().PushContext(
                    new Tenant { Id = settings.TenantId, Name = settings.TenantId });
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var tenantId = args.Length > 2 && command is "refresh" or "reconcile" or "disconnect" or "request-revocation" or "request-uninstall" or "reconcile-offboarding" or "cleanup"
                ? args[2]
                : settings.TenantId;
            var environmentId = args.Length > 3 && command is "refresh" or "reconcile" or "disconnect" or "request-revocation" or "request-uninstall" or "reconcile-offboarding" or "cleanup"
                ? args[3]
                : settings.EnvironmentId;

            switch (command)
            {
                case "connect":
                {
                    var lifecycle = services.GetRequiredService<IConnectionLifecycleService>();
                    var result = await lifecycle.ConnectAsync(
                        settings.Principal,
                        new ConnectConnectionRequest(
                            settings.TenantId,
                            settings.EnvironmentId,
                            "synthetic-oauth",
                            "synthetic-account",
                            new CredentialMaterial(
                                RequiredEnvironment("ELSA_TEST_INITIAL_ACCESS_TOKEN"),
                                RequiredEnvironment("ELSA_TEST_INITIAL_REFRESH_TOKEN"),
                                settings.TimeProvider.GetUtcNow().AddHours(1))));
                    return WriteResult(result);
                }
                case "refresh":
                {
                    var lifecycle = services.GetRequiredService<IConnectionLifecycleService>();
                    return WriteResult(await lifecycle.RefreshAsync(settings.Principal, tenantId, environmentId, RequiredArgument(args, 1)));
                }
                case "reconcile":
                {
                    var recovery = services.GetRequiredService<IConnectionLifecycleRecoveryService>();
                    return WriteResult(await recovery.ReconcileAsync(tenantId, environmentId, RequiredArgument(args, 1)));
                }
                case "disconnect":
                {
                    var lifecycle = services.GetRequiredService<IConnectionLifecycleService>();
                    return WriteResult(await lifecycle.DisconnectAsync(settings.Principal, tenantId, environmentId, RequiredArgument(args, 1)));
                }
                case "request-revocation":
                {
                    var lifecycle = services.GetRequiredService<IConnectionLifecycleService>();
                    return WriteResult(await lifecycle.RequestTokenRevocationAsync(
                        settings.Principal, tenantId, environmentId, RequiredArgument(args, 1), RequiredArgument(args, 4)));
                }
                case "request-uninstall":
                {
                    var lifecycle = services.GetRequiredService<IConnectionLifecycleService>();
                    return WriteResult(await lifecycle.RequestInstallationUninstallAsync(
                        settings.Principal, tenantId, environmentId, RequiredArgument(args, 1)));
                }
                case "reconcile-offboarding":
                {
                    var recovery = services.GetRequiredService<IConnectionLifecycleRecoveryService>();
                    return WriteResult(await recovery.ReconcileOffboardingAsync(tenantId, environmentId, RequiredArgument(args, 1)));
                }
                case "cleanup":
                {
                    var lifecycle = services.GetRequiredService<IConnectionLifecycleService>();
                    return WriteResult(await lifecycle.CleanupGenerationAsync(
                        settings.Principal, tenantId, environmentId, RequiredArgument(args, 1), RequiredArgument(args, 4)));
                }
                case "bind":
                {
                    var manager = services.GetRequiredService<IWorkflowCredentialBindingManager>();
                    return WriteResult(await manager.CreateAsync(settings.Principal, RequiredArgument(args, 1), RequiredArgument(args, 2)));
                }
                case "save-workflow-state":
                {
                    var workflowInstanceId = RequiredArgument(args, 1);
                    var logicalBindingId = RequiredArgument(args, 2);
                    var now = DateTimeOffset.UtcNow;
                    await services.GetRequiredService<IWorkflowInstanceStore>().SaveAsync(new WorkflowInstance
                    {
                        Id = workflowInstanceId,
                        TenantId = settings.TenantId,
                        DefinitionId = "credential-conformance-workflow",
                        DefinitionVersionId = "credential-conformance-workflow-v1",
                        Version = 1,
                        Status = WorkflowStatus.Running,
                        SubStatus = WorkflowSubStatus.Executing,
                        IsExecuting = true,
                        CreatedAt = now.AddHours(-1),
                        UpdatedAt = now.AddMinutes(-30),
                        WorkflowState = new WorkflowState
                        {
                            Id = workflowInstanceId,
                            DefinitionId = "credential-conformance-workflow",
                            DefinitionVersionId = "credential-conformance-workflow-v1",
                            DefinitionVersion = 1,
                            Status = WorkflowStatus.Running,
                            SubStatus = WorkflowSubStatus.Executing,
                            IsExecuting = true,
                            Input = new Dictionary<string, object> { ["credentialBinding"] = logicalBindingId }
                        }
                    });
                    return WriteResult(new { stored = true, workflowInstanceId });
                }
                case "inspect-workflow-state":
                {
                    var workflowInstanceId = RequiredArgument(args, 1);
                    var instance = await services.GetRequiredService<IWorkflowInstanceStore>().FindAsync(workflowInstanceId);
                    if (instance?.WorkflowState.Input.TryGetValue("credentialBinding", out var input) != true || input is not string logicalBindingId)
                    {
                        return WriteError("workflow_state_unavailable");
                    }

                    return WriteResult(new { workflowInstanceId, logicalBindingId });
                }
                case "restart-workflow":
                {
                    var workflowInstanceId = RequiredArgument(args, 1);
                    var tenantAccessor = services.GetRequiredService<DefaultTenantAccessor>();
                    var restarter = Substitute.For<IWorkflowRestarter>();
                    var observedTenant = string.Empty;
                    var resolved = false;
                    restarter.RestartWorkflowAsync(workflowInstanceId, Arg.Any<CancellationToken>())
                        .Returns(_ => ResolveDuringRestartAsync());

                    async Task ResolveDuringRestartAsync()
                    {
                        observedTenant = tenantAccessor.TenantId;
                        var instance = await services.GetRequiredService<IWorkflowInstanceStore>().FindAsync(workflowInstanceId);
                        if (instance?.WorkflowState.Input.TryGetValue("credentialBinding", out var input) != true || input is not string logicalBindingId)
                        {
                            return;
                        }

                        var fixture = new ActivityTestFixture(new WriteLine("credential restart"));
                        using var activityContext = await fixture.BuildAsync();
                        try
                        {
                            var credential = await services.GetRequiredService<IWorkflowCredentialResolver>()
                                .ResolveAsync(activityContext.WorkflowExecutionContext, logicalBindingId);
                            resolved = credential.AccessToken == Environment.GetEnvironmentVariable("ELSA_TEST_EXPECTED_ACCESS_TOKEN");
                        }
                        catch (ConnectionUnavailableException)
                        {
                            resolved = false;
                        }
                    }

                    var clock = Substitute.For<ISystemClock>();
                    clock.UtcNow.Returns(DateTimeOffset.UtcNow);
                    var tenantService = Substitute.For<ITenantService>();
                    tenantService.FindAsync(settings.TenantId, Arg.Any<CancellationToken>())
                        .Returns(new Tenant { Id = settings.TenantId, Name = settings.TenantId });
                    var task = new RestartInterruptedWorkflowsTask(
                        restarter,
                        services.GetRequiredService<IWorkflowInstanceStore>(),
                        services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RestartInterruptedWorkflowsTask>>(),
                        Options.Create(new RuntimeOptions
                        {
                            InactivityThreshold = TimeSpan.FromMinutes(5),
                            RestartInterruptedWorkflowsBatchSize = 10
                        }),
                        clock,
                        tenantService,
                        tenantAccessor);
                    await task.ExecuteAsync(CancellationToken.None);
                    return WriteResult(new
                    {
                        resolved,
                        observedTenant,
                        ambientTenantAfterRestart = tenantAccessor.TenantId
                    });
                }
                case "resolve-binding":
                {
                    var statePath = RequiredArgument(args, 1);
                    var stateJson = await File.ReadAllTextAsync(statePath);
                    using var document = JsonDocument.Parse(stateJson);
                    var logicalBindingId = document.RootElement.GetProperty("credentialBinding").GetString();
                    if (string.IsNullOrWhiteSpace(logicalBindingId))
                    {
                        return WriteError("logical_binding_missing");
                    }

                    var fixture = new ActivityTestFixture(new WriteLine("credential conformance"))
                        .ConfigureContext(context => context.WorkflowExecutionContext.Input["credentialBinding"] = logicalBindingId);
                    using var activityContext = await fixture.BuildAsync();
                    var resolver = services.GetRequiredService<IWorkflowCredentialResolver>();
                    try
                    {
                        var credential = await resolver.ResolveAsync(
                            activityContext.WorkflowExecutionContext,
                            (string)activityContext.WorkflowExecutionContext.Input["credentialBinding"]);
                        var expected = Environment.GetEnvironmentVariable("ELSA_TEST_EXPECTED_ACCESS_TOKEN");
                        return WriteResult(new { resolved = expected is not null && credential.AccessToken == expected });
                    }
                    catch (ConnectionUnavailableException)
                    {
                        return WriteResult(new { resolved = false, safeErrorCode = "connection_unavailable" });
                    }
                }
                case "inspect":
                {
                    var connectionId = RequiredArgument(args, 1);
                    var generationId = args.Length > 2 ? args[2] : null;
                    var store = services.GetRequiredService<IConnectionLifecycleStore>();
                    var connection = await store.FindAsync(connectionId, settings.TenantId, settings.EnvironmentId);
                    if (connection is null)
                    {
                        return WriteError("connection_unavailable");
                    }

                    var database = await services.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>().CreateDbContextAsync();
                    await using (database)
                    {
                        var operations = await database.OffboardingOperations.AsNoTracking()
                            .Where(x => x.ConnectionId == connectionId && x.TenantId == settings.TenantId && x.EnvironmentId == settings.EnvironmentId)
                            .OrderBy(x => x.CreatedAt)
                            .Select(x => new { x.Id, x.Kind, x.Status, x.GenerationId, x.AttemptCount, x.Fence, x.LastSafeErrorCode })
                            .ToArrayAsync();
                        var cleanup = generationId is null
                            ? null
                            : await store.FindGenerationCleanupAsync(connectionId, settings.TenantId, settings.EnvironmentId, generationId);
                        var generationAvailable = false;
                        if (generationId is not null)
                        {
                            try
                            {
                                var name = ManagedSecretNames.ForGeneration(connectionId, generationId);
                                await services.GetRequiredService<IManagedSecretManager>().ResolveGenerationAsync(name, connectionId, generationId);
                                generationAvailable = true;
                            }
                            catch (KeyNotFoundException)
                            {
                                generationAvailable = false;
                            }
                        }

                        return WriteResult(new
                        {
                            connection.Id,
                            status = connection.Status.ToString(),
                            connection.Revision,
                            operationStatus = connection.OperationStatus.ToString(),
                            connection.CurrentGenerationId,
                            connection.PlannedGenerationId,
                            connection.StagedGenerationId,
                            cleanupStatus = cleanup?.Status.ToString(),
                            generationAvailable,
                            operations = operations.Select(operation => new
                            {
                                operation.Id,
                                kind = operation.Kind.ToString(),
                                status = operation.Status.ToString(),
                                operation.GenerationId,
                                operation.AttemptCount,
                                operation.Fence,
                                operation.LastSafeErrorCode
                            })
                        });
                    }
                }
                default:
                    return WriteError("command_unknown");
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"WORKER_FAILURE:{exception.GetType().Name}");
            return 2;
        }
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var settings = WorkerSettings.FromEnvironment();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var tenantAccessor = new DefaultTenantAccessor();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<DefaultTenantAccessor>(tenantAccessor);
        services.AddSingleton<ITenantAccessor>(tenantAccessor);
        services.Configure<TenantsOptions>(options => options.IsEnabled = false);
        services.AddSingleton<TimeProvider>(settings.TimeProvider);
        services.AddSingleton<IConnectionUseAuthorizer, WorkerConnectionUseAuthorizer>();
        services.AddSingleton<IConnectionCredentialBindingUseAuthorizer, WorkerBindingUseAuthorizer>();
        services.AddSingleton<IConnectionCredentialBindingManagementAuthorizer, WorkerBindingManagementAuthorizer>();
        services.AddSingleton<SyntheticHttpCredentialProvider>();
        services.AddSingleton<IConnectionCredentialProvider>(provider => provider.GetRequiredService<SyntheticHttpCredentialProvider>());
        services.AddSingleton<IConnectionOffboardingProvider>(provider => provider.GetRequiredService<SyntheticHttpCredentialProvider>());

        var module = services.CreateModule();
        var secretsFeature = module.Configure<SecretsFeature>();
        secretsFeature.ConfigureOptions = options => options.EncryptionKey = settings.EncryptionKey;
        secretsFeature.UseEntityFrameworkCore(feature => feature.UsePostgreSql(settings.ConnectionString));
        module.Configure<ConnectionsFeature>();
        module.Configure<EFCoreConnectionsPersistenceFeature>(feature => feature.UsePostgreSql(settings.ConnectionString));
        module.Configure<WorkflowCredentialBindingsFeature>(feature => feature.EnvironmentId = settings.EnvironmentId);
        module.Configure<WorkflowManagementFeature>();
        module.Configure<EFCoreWorkflowInstancePersistenceFeature>(feature => feature.UsePostgreSql(settings.ConnectionString));
        module.Apply();

        services.Replace(ServiceDescriptor.Scoped<IConnectionLifecycleStore>(provider =>
        {
            var proxy = DispatchProxy.Create<IConnectionLifecycleStore, ProcessBoundaryConnectionLifecycleStore>();
            ((ProcessBoundaryConnectionLifecycleStore)(object)proxy).Initialize(
                provider.GetRequiredService<EFCoreConnectionLifecycleStore>());
            return proxy;
        }));

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static async Task MigrateAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        await using var connections = await services.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>().CreateDbContextAsync();
        await connections.Database.MigrateAsync();
        await using var secrets = await services.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>().CreateDbContextAsync();
        await secrets.Database.MigrateAsync();
        await using var management = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();
        await management.Database.MigrateAsync();
    }

    private static string RequiredEnvironment(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : throw new InvalidOperationException("required_environment_missing");

    private static string RequiredArgument(string[] args, int index) =>
        args.Length > index && !string.IsNullOrWhiteSpace(args[index]) ? args[index] : throw new InvalidOperationException("required_argument_missing");

    private static int WriteResult(object result)
    {
        Console.WriteLine($"RESULT:{JsonSerializer.Serialize(result, JsonOptions)}");
        return 0;
    }

    private static int WriteError(string safeErrorCode)
    {
        Console.WriteLine($"RESULT:{JsonSerializer.Serialize(new { succeeded = false, safeErrorCode }, JsonOptions)}");
        return 2;
    }
}

internal sealed record WorkerSettings(
    string ConnectionString,
    string TenantId,
    string EnvironmentId,
    string ProviderBaseAddress,
    byte[] EncryptionKey,
    TimeProvider TimeProvider)
{
    public ClaimsPrincipal Principal
    {
        get
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Environment.GetEnvironmentVariable("ELSA_TEST_SUBJECT") ?? "synthetic-worker"),
                new("tenant", TenantId),
                new("environment", EnvironmentId)
            };
            claims.AddRange((Environment.GetEnvironmentVariable("ELSA_TEST_PERMISSIONS") ?? "connections.manage,connections.use")
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(permission => new Claim("permission", permission)));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "synthetic-process"));
        }
    }

    public static WorkerSettings FromEnvironment()
    {
        var fixedNow = DateTimeOffset.Parse(
            Environment.GetEnvironmentVariable("ELSA_TEST_NOW_UTC") ?? DateTimeOffset.UtcNow.ToString("O"),
            System.Globalization.CultureInfo.InvariantCulture);
        return new WorkerSettings(
            Required("ELSA_TEST_CONNECTION_STRING"),
            Required("ELSA_TEST_TENANT_ID"),
            Required("ELSA_TEST_ENVIRONMENT_ID"),
            Required("ELSA_TEST_PROVIDER_ADDRESS"),
            Convert.FromBase64String(Required("ELSA_TEST_ENCRYPTION_KEY_BASE64")),
            new FixedTimeProvider(fixedNow));
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : throw new InvalidOperationException("required_environment_missing");
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
