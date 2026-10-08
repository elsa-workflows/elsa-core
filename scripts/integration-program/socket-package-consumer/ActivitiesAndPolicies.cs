using System.Collections.Concurrent;
using System.Data.Common;
using System.Security.Claims;
using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Workflows.Contracts;
using Elsa.Connections.Models;
using Elsa.Mediator.Contracts;
using Elsa.Slack.Activities.Events;
using Elsa.Slack.SocketMode;
using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Notifications;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using SlackNet.WebApi;

namespace SocketPackageConsumer;

public sealed class Probe(string connectionString) : INotificationHandler<WorkflowExecuting>, INotificationHandler<WorkflowStarted>, INotificationHandler<ActivityExecuted>
{
    private int _executing;
    private int _started;
    private int _watch;
    public int Executing => Volatile.Read(ref _executing);
    public int Started => Volatile.Read(ref _started);
    public int WatchExecutions => Volatile.Read(ref _watch);
    public WorkflowExecutionContext? LastContext { get; private set; }
    internal NeverCalledProvider Provider { get; } = new();
    internal SafeLogs Logs { get; } = new();
    internal TaskCompletionSource ActivityEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource ReleaseActivity { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Suspended { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource CycleSettled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task HandleAsync(WorkflowExecuting notification, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _executing);
        LastContext = notification.WorkflowExecutionContext;
        return Task.CompletedTask;
    }

    public Task HandleAsync(WorkflowStarted notification, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _started);
        return Task.CompletedTask;
    }

    public async Task HandleAsync(ActivityExecuted notification, CancellationToken cancellationToken)
    {
        var context = notification.ActivityExecutionContext;
        if (context.Activity is WatchPublicChannelMessages watch)
        {
            var message = context.GetActivityOutput(() => watch.ReceivedMessage) as Message;
            Require.That(message != null && message.Channel == FixtureConstants.ChannelId && message.Text == FixtureConstants.MessageText &&
                message.ThreadTs == null && Equals(context.GetActivityOutput(() => watch.UserId), "U_HUMAN") &&
                Equals(context.GetActivityOutput(() => watch.MessageTimestamp), FixtureConstants.MessageTimestamp) &&
                Equals(context.GetActivityOutput(() => watch.ReplyThreadTimestamp), FixtureConstants.MessageTimestamp), "real-watch-output-mapping");
            // Persist only known synthetic scalar outputs through the actual workflow state, not a fake message source.
            context.WorkflowExecutionContext.Output["watchChannel"] = message!.Channel;
            context.WorkflowExecutionContext.Output["watchText"] = message.Text;
            context.WorkflowExecutionContext.Output["watchUser"] = "U_HUMAN";
            context.WorkflowExecutionContext.Output["watchTimestamp"] = FixtureConstants.MessageTimestamp;
            context.WorkflowExecutionContext.Output["watchReplyThread"] = FixtureConstants.MessageTimestamp;
            context.WorkflowExecutionContext.Output["watchThreadAbsent"] = true;
            Interlocked.Increment(ref _watch);
            await IncrementAsync("watch", cancellationToken);
        }
    }

    internal async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("CREATE TABLE \"Elsa\".\"SocketPackageEffects\" (\"Name\" text PRIMARY KEY, \"Value\" integer NOT NULL)", connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal async Task IncrementAsync(string name, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("INSERT INTO \"Elsa\".\"SocketPackageEffects\" AS c (\"Name\", \"Value\") VALUES (@name, 1) ON CONFLICT (\"Name\") DO UPDATE SET \"Value\" = c.\"Value\" + 1", connection);
        command.Parameters.AddWithValue("name", name);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal async Task<int> CountAsync(string name, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT \"Value\" FROM \"Elsa\".\"SocketPackageEffects\" WHERE \"Name\" = @name", connection);
        command.Parameters.AddWithValue("name", name);
        return await command.ExecuteScalarAsync(cancellationToken) is int count ? count : 0;
    }
}

[Activity(Namespace = "SocketPackage", Type = "Checkpoint", Category = "Fixture")]
public sealed class CheckpointActivity : Activity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var probe = context.GetRequiredService<Probe>();
        probe.ActivityEntered.TrySetResult();
        await probe.ReleaseActivity.Task.WaitAsync(context.CancellationToken);
        await probe.IncrementAsync("suspend", context.CancellationToken);
        context.CreateBookmark(ResumeAsync);
        probe.Suspended.TrySetResult();
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        await context.GetRequiredService<Probe>().IncrementAsync("resume", context.CancellationToken);
        await context.CompleteActivityAsync();
    }
}

// An actual database transaction barrier; only the Admission context is registered with it.
// Source: https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors
internal sealed class AdmissionCommitGate(EvidenceClock clock) : DbTransactionInterceptor
{
    private int _armed;
    private DbTransaction? _held;
    internal long CommittedOrder { get; private set; }
    internal string? HeldAdmissionId { get; private set; }
    internal int HeldCommits { get; private set; }
    internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal void Arm() => Interlocked.Exchange(ref _armed, 1);
    public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
        InterceptionResult result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is AdmissionElsaDbContext && Interlocked.Exchange(ref _armed, 0) == 1)
        {
            var row = ((AdmissionElsaDbContext)eventData.Context).ChangeTracker.Entries<AdmissionRecord>().Select(x => x.Entity).Single();
            Require.That(row.State == AdmissionState.Admitted && row.Revision == 1 && row.ProviderEventId == FixtureConstants.EventId &&
                row.WorkflowInstanceId == null, "held-commit-not-initial-admission");
            HeldAdmissionId = row.Id;
            _held = transaction;
            HeldCommits++;
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
        }
        return result;
    }

    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        if (ReferenceEquals(transaction, _held))
        {
            CommittedOrder = clock.Next();
        }
        return Task.CompletedTask;
    }
}

internal sealed class FixturePolicies : IConnectionUseAuthorizer, IConnectionCredentialBindingManagementAuthorizer
{
    internal string? ConnectionId { get; set; }
    internal static ClaimsPrincipal Principal() => new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "socket-package-fixture")], "socket-package-fixture"));
    public Task<bool> AuthorizeAsync(ConnectionUseRequest request, CancellationToken cancellationToken = default) => Task.FromResult(
        request.Kind == ConnectionUseKind.Human && request.Purpose == "manage:connect" && request.ConnectionId == "" &&
        request.TenantId == FixtureConstants.TenantId && request.EnvironmentId == FixtureConstants.EnvironmentId &&
        request.Principal.Identity?.IsAuthenticated == true && request.Principal.FindFirstValue(ClaimTypes.NameIdentifier) == "socket-package-fixture");
    public Task<bool> AuthorizeAsync(ClaimsPrincipal principal, ConnectionCredentialBindingManagementRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(principal.Identity?.IsAuthenticated == true && principal.FindFirstValue(ClaimTypes.NameIdentifier) == "socket-package-fixture" &&
            request.TenantId == FixtureConstants.TenantId && request.EnvironmentId == FixtureConstants.EnvironmentId &&
            request.LogicalBindingId == FixtureConstants.BindingId && ConnectionId != null && request.ConnectionId == ConnectionId && request.ExpectedRevision == null);
}

// Only the actual fixed internal listener principal/purpose is authorized. Public background access remains denied.
internal sealed class ListenerPolicies(SlackSocketModeConfiguration configuration) : IConnectionUseAuthorizer
{
    private int _calls;
    internal int Calls => Volatile.Read(ref _calls);
    public Task<bool> AuthorizeAsync(ConnectionUseRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _calls);
        return Task.FromResult(request.Kind == ConnectionUseKind.BackgroundSystem && request.Purpose == "listen:slack-socket" &&
            request.TenantId == configuration.TenantId && request.EnvironmentId == configuration.EnvironmentId && request.ConnectionId == configuration.ConnectionId &&
            request.Principal.Identity is { IsAuthenticated: true, AuthenticationType: "Elsa.Slack.SocketMode.Listener" } &&
            request.Principal.FindFirstValue(ClaimTypes.NameIdentifier) == "elsa-slack-socket-listener" &&
            request.Principal.FindFirstValue("elsa:identity-kind") == "system");
    }
}

internal sealed class NeverCalledProvider : IConnectionCredentialProvider
{
    private int _calls;
    public int Calls => Volatile.Read(ref _calls);
    public Task<CredentialMaterial> RefreshAsync(string providerId, string accountId, string refreshToken, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _calls);
        return Task.FromException<CredentialMaterial>(new ProofFailure("provider-must-not-be-called"));
    }
}

internal sealed class SafeLogs : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _messages = new();
    public ILogger CreateLogger(string categoryName) => new Logger(_messages);
    public void Dispose() { }
    public void AssertClean()
    {
        foreach (var message in _messages)
        {
            Require.NoSecret(message);
        }
    }
    private sealed class Logger(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            messages.Enqueue(formatter(state, exception));
    }
}

// This scoped observer signals only after the actual durable batch scope disposes,
// which is after ExecuteAsync has returned and released its local owner. No polling or retry-resume.
internal sealed class BatchScopeObserver(Probe probe) : IAdmissionExecutionObserver, IAsyncDisposable
{
    private bool _checkpoint;
    public ValueTask ObserveAsync(AdmissionExecutionBoundary boundary, string admissionId, string workflowInstanceId, CancellationToken cancellationToken)
    {
        if (boundary == AdmissionExecutionBoundary.CheckpointRecorded)
        {
            _checkpoint = true;
        }
        return ValueTask.CompletedTask;
    }
    public ValueTask DisposeAsync()
    {
        if (_checkpoint)
        {
            probe.CycleSettled.TrySetResult();
        }
        return ValueTask.CompletedTask;
    }
}
