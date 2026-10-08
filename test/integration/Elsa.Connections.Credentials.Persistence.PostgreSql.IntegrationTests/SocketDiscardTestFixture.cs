using System.Data.Common;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Slack.SocketMode.Persistence;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Elsa.Workflows.Admission.Persistence.EFCore.Features;
using Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql.Extensions;
using Elsa.Workflows.Admission.WorkerProcess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

internal sealed class SocketDiscardTestFixture(ServiceProvider services) : IAsyncDisposable
{
    internal ServiceProvider Services => services;
    internal IAdmissionStore Admissions => services.GetRequiredService<IAdmissionStore>();
    internal ISlackSocketDiscardStore Discards => services.GetRequiredService<ISlackSocketDiscardStore>();
    internal static DateTimeOffset Now => AdmissionWorkerHost.Now;
    internal static string Authority => AdmissionWorkerHost.Configuration().Policy.CleanupAuthority;

    internal static async Task<SocketDiscardTestFixture> CreateAsync(PostgreSqlConnectionsFixture fixture,
        AdmissionSubscriptionConfiguration? configuration = null, IInterceptor[]? interceptors = null, bool migrateReceipts = true,
        string? receiptHistory = null, string? receiptSchema = null, string? receiptConnectionString = null, bool validateProvisioning = true, bool migrateAdmission = true, bool receiptRetries = false)
    {
        await fixture.ResetSchemaAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var module = services.CreateModule();
        module.Configure<EFCoreAdmissionPersistenceFeature>(feature =>
        {
            feature.TenantId = AdmissionWorkerHost.TenantId;
            feature.EnvironmentId = AdmissionWorkerHost.EnvironmentId;
            feature.RunMigrations = false;
            feature.UsePostgreSql(fixture.ConnectionString);
            if (interceptors != null)
            {
                var configure = feature.DbContextOptionsBuilder;
                feature.DbContextOptionsBuilder = (provider, builder) =>
                {
                    configure(provider, builder);
                    builder.AddInterceptors(interceptors);
                };
            }
        });
        module.Apply();
        services.AddDbContextFactory<SlackSocketReceiptElsaDbContext>((_, builder) =>
        {
            builder.UseElsaPostgreSql(typeof(SlackSocketReceiptElsaDbContext).Assembly, receiptConnectionString ?? fixture.ConnectionString,
                new ElsaDbContextOptions { MigrationsHistoryTableName = receiptHistory ?? SlackSocketReceiptElsaDbContext.HistoryTable, SchemaName = receiptSchema },
                provider =>
                {
                    if (receiptRetries)
                    {
                        provider.EnableRetryOnFailure();
                    }
                });
            if (interceptors != null)
            {
                builder.AddInterceptors(interceptors);
            }
        });
        services.AddSlackSocketDiscardPersistence();
        var host = new SocketDiscardTestFixture(services.BuildServiceProvider());
        try
        {
            if (migrateAdmission)
            {
                await AdmissionWorkerHost.MigrateAsync(host.Services);
            }
            if (migrateReceipts)
            {
                await using var receipts = await host.ReceiptContextAsync();
                await receipts.Database.MigrateAsync();
                if (validateProvisioning)
                {
                    await host.Discards.ValidateProvisioningAsync();
                }
            }
            await AdmissionTestLedger.ActivateAsync(host.Admissions, configuration ?? AdmissionWorkerHost.Configuration());
            return host;
        }
        catch
        {
            await host.DisposeAsync();
            throw;
        }
    }

    internal Task<AdmissionElsaDbContext> AdmissionContextAsync() => services.GetRequiredService<IDbContextFactory<AdmissionElsaDbContext>>().CreateDbContextAsync();
    internal Task<SlackSocketReceiptElsaDbContext> ReceiptContextAsync() => services.GetRequiredService<IDbContextFactory<SlackSocketReceiptElsaDbContext>>().CreateDbContextAsync();
    internal async Task<AdmissionSubscription> SubscriptionAsync() => (await Admissions.FindSubscriptionAsync(AdmissionWorkerHost.Configuration().Id))!;

    internal async Task<SlackSocketDiscardRequest> RequestAsync(string eventId = "discard-event", SlackSocketDiscardReason reason = SlackSocketDiscardReason.Bot)
    {
        var subscription = await SubscriptionAsync();
        return new(AdmissionWorkerHost.Event(eventId) with { IsHumanMessage = false, Payload = "bounded-normalized-discard" }, reason,
            AdmissionHash.Compute("synthetic-immutable-listener-binding"), subscription.ConfigurationFingerprint, subscription.ActivationEpoch);
    }

    internal async Task<SlackSocketDiscardReceipt?> ReceiptAsync(string id)
    {
        await using var receipts = await ReceiptContextAsync();
        return await receipts.Receipts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
    }

    internal static async Task ReleaseAndAwaitAsync(TaskCompletionSource release, params Task?[] operations)
    {
        release.TrySetResult();
        await Task.WhenAll(operations.OfType<Task>());
    }

    public ValueTask DisposeAsync() => services.DisposeAsync();
}

/// <summary>Holds an actual first commit while a second transaction reaches the shared advisory lock.</summary>
internal sealed class SocketDiscardCommitGate : DbTransactionInterceptor
{
    private int _armed;
    private int _commits;
    internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal void Arm() => Volatile.Write(ref _armed, 1);

    public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
        InterceptionResult result, CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _armed) == 1 && Interlocked.Increment(ref _commits) == 1)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        }
        return result;
    }
}

internal sealed class SocketDiscardLockObserver : DbCommandInterceptor
{
    private int _armed;
    private int _attempts;
    internal TaskCompletionSource Contender { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal void Arm() => Volatile.Write(ref _armed, 1);

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _armed) == 1 && command.CommandText.Contains("pg_advisory_xact_lock", StringComparison.Ordinal) &&
            Interlocked.Increment(ref _attempts) == 2)
        {
            Contender.TrySetResult();
        }
        return ValueTask.FromResult(result);
    }
}

internal sealed class SocketDiscardUnknownCommit : DbTransactionInterceptor
{
    private int _armed;
    internal void Arm() => Volatile.Write(ref _armed, 1);

    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _armed, 0) == 1)
        {
            throw new InvalidOperationException("socket_fixture_commit_response_unknown");
        }
        return Task.CompletedTask;
    }
}

internal sealed class SocketDiscardCounterSaveFailure : SaveChangesInterceptor
{
    private int _armed;
    internal void Arm() => Volatile.Write(ref _armed, 1);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is AdmissionElsaDbContext && Interlocked.Exchange(ref _armed, 0) == 1)
        {
            throw new InvalidOperationException("socket_fixture_counter_save_failed");
        }
        return ValueTask.FromResult(result);
    }
}

/// <summary>Captures actual commands on both contexts, without retaining SQL or parameters.</summary>
internal sealed class SocketDiscardTransactionObserver : DbCommandInterceptor
{
    internal DbConnection? AdmissionConnection { get; private set; }
    internal DbTransaction? AdmissionTransaction { get; private set; }
    internal DbConnection? ReceiptConnection { get; private set; }
    internal DbTransaction? ReceiptTransaction { get; private set; }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.StartsWith("UPDATE ", StringComparison.Ordinal) && eventData.Context is AdmissionElsaDbContext)
        {
            AdmissionConnection = command.Connection;
            AdmissionTransaction = command.Transaction;
        }
        if (command.CommandText.StartsWith("INSERT ", StringComparison.Ordinal) && eventData.Context is SlackSocketReceiptElsaDbContext)
        {
            ReceiptConnection = command.Connection;
            ReceiptTransaction = command.Transaction;
        }
        return ValueTask.FromResult(result);
    }
}

/// <summary>Pauses the duplicate lookup after the subscription lock is held.</summary>
internal sealed class SocketDiscardDuplicateReadGate : DbCommandInterceptor
{
    private int _armed;
    internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal void Arm() => Volatile.Write(ref _armed, 1);

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is SlackSocketReceiptElsaDbContext &&
            command.CommandText.Contains("FROM \"Elsa\".\"SlackSocketDiscardReceipts\"", StringComparison.Ordinal) &&
            Interlocked.Exchange(ref _armed, 0) == 1)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        }
        return result;
    }
}
