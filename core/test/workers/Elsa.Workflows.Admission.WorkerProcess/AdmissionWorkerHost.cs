using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Elsa.Workflows.Admission.Persistence.EFCore.Features;
using Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Admission.WorkerProcess;

/// <summary>Real selected-provider commands. This process is not a second workflow executor.</summary>
public static class AdmissionWorkerHost
{
    public const string TenantId = "admission-fixture-tenant";
    public const string EnvironmentId = "admission-fixture-environment";
    public static readonly DateTimeOffset Now = new(2026, 10, 8, 0, 0, 0, TimeSpan.Zero);

    public static AdmissionSubscriptionConfiguration Configuration(int activeCapacity = 4, int retainedCapacity = 8, string id = "admission-fixture-subscription") =>
        new(id, TenantId, EnvironmentId, "installation-stable", "channel-fixed", "definition-fixed", "definition-version-fixed", 1,
            AdmissionHash.Compute("synthetic-fixed-artifact"), Now.AddMinutes(-1),
            new(TimeSpan.FromHours(1), TimeSpan.FromDays(2), TimeSpan.FromDays(1), TimeSpan.FromMinutes(1), activeCapacity,
                retainedCapacity, 4096, 256, AdmissionRejectedEventDisposition.Quarantine, AdmissionRejectedEventDisposition.Reject, "fixture-cleanup-authority"));

    public static AdmissionEvent Event(string eventId = "event-1", string subscriptionId = "admission-fixture-subscription") =>
        new(subscriptionId, "installation-stable", "channel-fixed", eventId, Now, true, false, "synthetic-human-message");

    public static ServiceProvider CreateServices(string connectionString, AdmissionPersistenceScope? scope = null, IInterceptor? interceptor = null, bool includeManagement = false)
    {
        scope ??= new(TenantId, EnvironmentId);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var module = services.CreateModule();
        module.Configure<EFCoreAdmissionPersistenceFeature>(feature =>
        {
            feature.TenantId = scope.TenantId;
            feature.EnvironmentId = scope.EnvironmentId;
            feature.RunMigrations = false;
            feature.UsePostgreSql(connectionString);
            if (interceptor != null)
            {
                var configure = feature.DbContextOptionsBuilder;
                feature.DbContextOptionsBuilder = (provider, builder) =>
                {
                    configure(provider, builder);
                    builder.AddInterceptors(interceptor);
                };
            }
        });
        if (includeManagement)
        {
            module.Configure<WorkflowManagementFeature>();
            module.Configure<EFCoreWorkflowDefinitionPersistenceFeature>(feature => feature.UsePostgreSql(connectionString));
        }
        module.Apply();
        return services.BuildServiceProvider();
    }

    public static async Task MigrateAsync(IServiceProvider services)
    {
        await using var db = await services.GetRequiredService<IDbContextFactory<AdmissionElsaDbContext>>().CreateDbContextAsync();
        await db.Database.MigrateAsync();
    }

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var connection = Environment.GetEnvironmentVariable("ELSA_TEST_CONNECTION_STRING") ?? throw new InvalidOperationException("connection_required");
            var boundary = Environment.GetEnvironmentVariable("ELSA_ADMISSION_COMMIT_BOUNDARY");
            await using var services = CreateServices(connection, interceptor: boundary == null ? null : new AdmissionCommitBarrier(boundary), includeManagement: args[0] == "bootstrap-insert");
            if (Environment.GetEnvironmentVariable("ELSA_ADMISSION_START_GATE") is { } startGate)
            {
                Console.WriteLine("BARRIER_READY");
                await Console.Out.FlushAsync();
                await AdmissionFileGate.WaitAsync(startGate);
            }
            var store = services.GetRequiredService<IAdmissionStore>();
            object result;
            switch (args[0])
            {
                case "bootstrap-insert":
                {
                    var definition = Artifact();
                    var fingerprint = AdmissionDefinitionFingerprint.Compute(definition, services.GetRequiredService<IPayloadSerializer>());
                    var configuration = Configuration(id: args[1]) with { DefinitionFingerprint = fingerprint };
                    var bootstrap = services.GetRequiredService<IAdmissionDefinitionBootstrapStore>();
                    Console.WriteLine("BOUNDARY:bootstrap-acquiring");
                    await Console.Out.FlushAsync();
                    await using var lease = await bootstrap.AcquireExclusiveAsync(configuration);
                    Console.WriteLine("BOUNDARY:bootstrap-exclusive");
                    await Console.Out.FlushAsync();
                    if (Environment.GetEnvironmentVariable("ELSA_ADMISSION_BOOTSTRAP_GATE") is { } bootstrapGate)
                    {
                        await AdmissionFileGate.WaitAsync(bootstrapGate);
                    }
                    var inserted = await bootstrap.InsertOrVerifyAsync(definition, fingerprint);
                    result = new { outcome = inserted.ToString() };
                    break;
                }
                case "inspect":
                    result = Snapshot(await store.FindAsync(args[1]));
                    break;
                case "admit":
                    var admitted = await store.AdmitAsync(Event(args[1]), Now);
                    result = new { outcome = admitted.Outcome.ToString(), admitted.AdmissionId, admitted.Revision, admitted.AcknowledgementEligible };
                    break;
                case "authorize":
                    result = Snapshot(await store.AuthorizeStartAsync(args[1], long.Parse(args[2]), args[3]));
                    break;
                case "withdraw":
                    var subscription = await store.WithdrawAsync(Configuration().Id, long.Parse(args[1]), false, null);
                    result = new { changed = subscription != null, subscription?.Revision, subscription?.Active };
                    break;
                case "cleanup":
                    result = new { cleaned = await store.CleanupAsync(args[1], long.Parse(args[2]), Configuration().Policy.CleanupAuthority, DateTimeOffset.Parse(args[3])) };
                    break;
                default:
                    throw new InvalidOperationException("unknown_command");
            }
            Console.WriteLine("RESULT:" + JsonSerializer.Serialize(new
            {
                processId = Environment.ProcessId, processStartedAt = Process.GetCurrentProcess().StartTime.ToUniversalTime(), result
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            return 0;
        }
        catch
        {
            // Exceptions can contain connection strings/payloads; export a fixed allowlisted classification only.
            Console.WriteLine("ERROR:admission_operation_not_definitive");
            return 2;
        }
    }

    public static WorkflowDefinition Artifact() => new()
    {
        Id = "definition-version-fixed", DefinitionId = "definition-fixed", TenantId = TenantId,
        Version = 1, CreatedAt = Now, IsLatest = true, MaterializerName = "Json", StringData = "{}"
    };

    private static object Snapshot(AdmissionRecord? record) => new
    {
        changed = record != null, record?.Id, record?.Revision, state = record?.State.ToString(), record?.AuthorityOutstanding, record?.ActivationEpoch
    };
}

internal static class AdmissionFileGate
{
    public static async Task WaitAsync(string path)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var watcher = new FileSystemWatcher(Path.GetDirectoryName(path)!, Path.GetFileName(path));
        watcher.Created += (_, _) => completion.TrySetResult();
        watcher.Renamed += (_, _) => completion.TrySetResult();
        watcher.EnableRaisingEvents = true;
        if (File.Exists(path))
        {
            return;
        }
        await completion.Task.WaitAsync(timeout.Token);
    }
}

internal sealed class AdmissionCommitBarrier(string boundary) : DbTransactionInterceptor
{
    public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
        InterceptionResult result, CancellationToken cancellationToken = default)
    {
        if (boundary == "before-commit")
        {
            await WaitAsync();
        }
        return result;
    }

    public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        if (boundary is "after-commit" or "unknown-commit")
        {
            await WaitAsync();
            if (boundary == "unknown-commit")
            {
                throw new IOException("fixture_lost_commit_response");
            }
        }
    }

    private async Task WaitAsync()
    {
        Console.WriteLine("BOUNDARY:" + boundary);
        await Console.Out.FlushAsync();
        await AdmissionFileGate.WaitAsync(Environment.GetEnvironmentVariable("ELSA_ADMISSION_COMMIT_GATE")!);
    }
}
