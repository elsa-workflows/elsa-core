using System.Collections.Concurrent;
using System.Security.Claims;
using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Workflows.Contracts;
using Elsa.Connections.Models;
using Elsa.Connections.Services;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AdmissionPackageConsumer;

public sealed class Probe(string connectionString) : INotificationHandler<WorkflowExecuting>, INotificationHandler<WorkflowStarted>
{
    public WorkflowExecutionContext? LastContext { get; private set; }
    public int ExecutingNotifications { get; private set; }
    public int StartedNotifications { get; private set; }
    internal NeverCalledProvider Provider { get; } = new();
    internal SafeLogs Logs { get; } = new();

    public Task HandleAsync(WorkflowExecuting notification, CancellationToken cancellationToken)
    {
        ExecutingNotifications++;
        LastContext = notification.WorkflowExecutionContext;
        return Task.CompletedTask;
    }

    public Task HandleAsync(WorkflowStarted notification, CancellationToken cancellationToken)
    {
        StartedNotifications++;
        return Task.CompletedTask;
    }

    internal async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("CREATE TABLE \"Elsa\".\"PackageConsumerEffects\" (\"Name\" text PRIMARY KEY, \"Value\" integer NOT NULL)", connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal async Task IncrementAsync(string name, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("INSERT INTO \"Elsa\".\"PackageConsumerEffects\" AS counters (\"Name\", \"Value\") VALUES (@name, 1) ON CONFLICT (\"Name\") DO UPDATE SET \"Value\" = counters.\"Value\" + 1", connection);
        command.Parameters.AddWithValue("name", name);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal async Task<int> CountAsync(string name, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT \"Value\" FROM \"Elsa\".\"PackageConsumerEffects\" WHERE \"Name\" = @name", connection);
        command.Parameters.AddWithValue("name", name);
        return await command.ExecuteScalarAsync(cancellationToken) is int count ? count : 0;
    }
}

[Activity(Namespace = "PackageConsumer", Type = "Admission", Category = "Fixture")]
public sealed class AdmissionActivity : Activity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await context.GetRequiredService<Probe>().IncrementAsync("admissionExecute", context.CancellationToken);
        context.CreateBookmark(ResumeAsync);
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        await context.GetRequiredService<Probe>().IncrementAsync("admissionResume", context.CancellationToken);
        await context.CompleteActivityAsync();
    }
}

public sealed class GrantWorkflow : WorkflowBase
{
    public const string DefinitionId = "package-grant-workflow";
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.WithTenantId(FixtureConstants.TenantId);
        builder.Root = new GrantActivity { Id = "package-grant-activity" };
    }
}

[Activity(Namespace = "PackageConsumer", Type = "Grant", Category = "Fixture")]
public sealed class GrantActivity : Activity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await context.GetRequiredService<Probe>().IncrementAsync("grantSuspend", context.CancellationToken);
        context.CreateBookmark(ResumeAsync);
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var probe = context.GetRequiredService<Probe>();
        await probe.IncrementAsync("grantAttempt", context.CancellationToken);
        try
        {
            var credential = await context.GetRequiredService<IWorkflowCredentialResolver>()
                .ResolveAsync(context.WorkflowExecutionContext, FixtureConstants.BindingId, context.CancellationToken);
            Require.That(credential.Kind == ConnectionCredentialKind.ApiKey && credential.AccessToken == FixtureConstants.SecretMarker,
                "credential-resolution-not-matching");
            await probe.IncrementAsync("grantSuccess", context.CancellationToken);
            await probe.IncrementAsync("grantSuccess:" + context.WorkflowExecutionContext.Id, context.CancellationToken);
        }
        catch (ConnectionUnavailableException)
        {
            await probe.IncrementAsync("grantDenied", context.CancellationToken);
            await probe.IncrementAsync("grantDenied:" + context.WorkflowExecutionContext.Id, context.CancellationToken);
        }
        await context.CompleteActivityAsync();
    }
}

internal sealed class FixturePolicies : IConnectionUseAuthorizer, IConnectionCredentialBindingManagementAuthorizer,
    IConnectionCredentialGrantManagementAuthorizer, IConnectionCredentialShareAuthorizer
{
    public string? ConnectionId { get; set; }
    public string? GrantedInstanceId { get; set; }
    public long BindingRevision { get; set; }
    public static ClaimsPrincipal Principal() => new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "package-fixture-actor")], "package-fixture"));
    private static bool Actor(ClaimsPrincipal principal) => principal.Identity?.IsAuthenticated == true &&
        principal.FindFirstValue(ClaimTypes.NameIdentifier) == "package-fixture-actor";
    private static bool Scope(string tenant, string environment) => tenant == FixtureConstants.TenantId && environment == FixtureConstants.EnvironmentId;

    public Task<bool> AuthorizeAsync(ConnectionUseRequest request, CancellationToken cancellationToken = default) => Task.FromResult(
        Scope(request.TenantId, request.EnvironmentId) &&
        ((request.Kind == ConnectionUseKind.Human && request.Purpose == "manage:connect" && request.ConnectionId == "" && Actor(request.Principal)) ||
         (request.Kind == ConnectionUseKind.BackgroundSystem && request.Purpose == "use" && ConnectionId != null && request.ConnectionId == ConnectionId)));

    public Task<bool> AuthorizeAsync(ClaimsPrincipal principal, ConnectionCredentialBindingManagementRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Actor(principal) && Scope(request.TenantId, request.EnvironmentId) && request.LogicalBindingId == FixtureConstants.BindingId &&
            ConnectionId != null && request.ConnectionId == ConnectionId && request.ExpectedRevision == null);

    public Task<bool> AuthorizeAsync(ClaimsPrincipal principal, ConnectionCredentialGrantManagementRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Actor(principal) && Scope(request.TenantId, request.EnvironmentId) && request.LogicalBindingId == FixtureConstants.BindingId &&
            ConnectionId != null && request.ConnectionId == ConnectionId && request.WorkflowInstanceId == GrantedInstanceId &&
            request.BindingRevision == BindingRevision && request.Action == ConnectionCredentialGrantAction.Issue);

    public Task<bool> AuthorizeAsync(ClaimsPrincipal principal, ConnectionCredentialShareRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Actor(principal) && Scope(request.TenantId, request.EnvironmentId) && request.LogicalBindingId == FixtureConstants.BindingId &&
            ConnectionId != null && request.ConnectionId == ConnectionId && request.WorkflowInstanceId == GrantedInstanceId && request.BindingRevision == BindingRevision);
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
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            messages.Enqueue(formatter(state, exception));
        }
    }
}
