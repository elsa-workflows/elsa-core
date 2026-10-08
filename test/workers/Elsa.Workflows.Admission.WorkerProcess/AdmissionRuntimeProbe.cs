using System.Collections.Concurrent;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Elsa.Workflows.Admission.WorkerProcess;

/// <summary>Trusted, fixed fixture instrumentation. Gates never convey a capability or context.</summary>
public sealed class AdmissionRuntimeProbe(string connectionString) : IAdmissionExecutionObserver,
    INotificationHandler<WorkflowExecuting>, INotificationHandler<WorkflowStarted>, INotificationHandler<WorkflowInstanceSaved>, INotificationHandler<WorkflowCancelling>,
    INotificationHandler<WorkflowDefinitionRetracting>, INotificationHandler<WorkflowDefinitionPublished>
{
    public string Outcome { get; set; } = "completed";
    public Func<string, Task>? Boundary { get; set; }
    public Func<WorkflowExecutionContext, Task>? OnExecuting { get; set; }
    public Func<WorkflowExecutionContext, Task>? OnRestored { get; set; }
    public WorkflowExecutionContext? PreparedContext { get; set; }
    public bool EnableVariable { get; set; }
    public bool ResuspendOnResume { get; set; }
    public bool FailSavedNotification { get; set; }
    public bool FailRetract { get; set; }
    public bool FailPublication { get; set; }
    private readonly ConcurrentDictionary<string, int> _counts = new(StringComparer.Ordinal);
    public int Count(string key) => _counts.GetValueOrDefault(key);

    public async Task InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS \"Elsa\".\"AdmissionRuntimeProofCounters\" (\"Name\" text PRIMARY KEY, \"Value\" integer NOT NULL)", connection);
        await command.ExecuteNonQueryAsync();
        await using var variables = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS \"Elsa\".\"AdmissionRuntimeProofVariables\" (\"InstanceId\" text NOT NULL, \"VariableId\" text NOT NULL, \"Value\" text NOT NULL, PRIMARY KEY (\"InstanceId\", \"VariableId\"))", connection);
        await variables.ExecuteNonQueryAsync();
    }

    public async Task IncrementAsync(string key)
    {
        _counts.AddOrUpdate(key, 1, (_, count) => count + 1);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("INSERT INTO \"Elsa\".\"AdmissionRuntimeProofCounters\" AS counters (\"Name\", \"Value\") VALUES (@name, 1) ON CONFLICT (\"Name\") DO UPDATE SET \"Value\" = counters.\"Value\" + 1", connection);
        command.Parameters.AddWithValue("name", key);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> ReadDurableCountAsync(string key)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT \"Value\" FROM \"Elsa\".\"AdmissionRuntimeProofCounters\" WHERE \"Name\" = @name", connection);
        command.Parameters.AddWithValue("name", key);
        return (await command.ExecuteScalarAsync()) is int count ? count : 0;
    }

    public async ValueTask ObserveAsync(AdmissionExecutionBoundary boundary, string admissionId, string instanceId, CancellationToken cancellationToken)
    {
        await IncrementAsync(boundary.ToString());
        await ReachAsync(boundary.ToString());
    }
    public Task ReachAsync(string boundary) => Boundary?.Invoke(boundary) ?? Task.CompletedTask;
    public async Task HandleAsync(WorkflowExecuting notification, CancellationToken cancellationToken)
    {
        await IncrementAsync("workflowExecuting");
        if (OnExecuting != null)
        {
            await OnExecuting(notification.WorkflowExecutionContext);
        }
    }
    public Task HandleAsync(WorkflowStarted notification, CancellationToken cancellationToken) => IncrementAsync("workflowStarted");
    public Task HandleAsync(WorkflowCancelling notification, CancellationToken cancellationToken) => IncrementAsync("workflowCancelling");
    public async Task HandleAsync(WorkflowDefinitionRetracting notification, CancellationToken cancellationToken)
    {
        await IncrementAsync("definitionRetracting");
        await ReachAsync("DefinitionRetracting");
        if (FailRetract)
        {
            throw new IOException("fixture_retract_outcome_unknown");
        }
    }
    public async Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        await IncrementAsync("definitionPublished");
        if (FailPublication)
        {
            throw new IOException("fixture_publication_outcome_unknown");
        }
    }
    public async Task HandleAsync(WorkflowInstanceSaved notification, CancellationToken cancellationToken)
    {
        await IncrementAsync("savedNotifications");
        if (FailSavedNotification)
        {
            throw new IOException("fixture_saved_notification_unknown");
        }
    }
}

[Activity(Namespace = "AdmissionProof", Type = "RuntimeProbe", Category = "Tests")]
public sealed class AdmissionRuntimeActivity : Activity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var probe = context.GetRequiredService<AdmissionRuntimeProbe>();
        await probe.IncrementAsync("activityEffects");
        await probe.ReachAsync("ActivityEffect");
        if (probe.Outcome == "faulted")
        {
            throw new InvalidOperationException("fixture_handled_activity_fault");
        }
        if (probe.Outcome == "suspended")
        {
            if (probe.EnableVariable)
            {
                // Retain the SAME variable/driver binding in both durable DynamicVariables
                // and its memory block; restored contexts must select the external driver.
                var container = context.FindParentWithVariableContainer()
                    ?? throw new InvalidOperationException("fixture_variable_container_missing");
                var variable = new Variable<string>("DurableProof", "saved-variable")
                {
                    StorageDriverType = typeof(AdmissionProofStorageDriver)
                };
                container.DynamicVariables.Add(variable);
                container.ExpressionExecutionContext.Memory.Declare(variable);
            }
            context.CreateBookmark(ResumeAsync);
            return;
        }
        await context.CompleteActivityAsync();
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var probe = context.GetRequiredService<AdmissionRuntimeProbe>();
        await probe.IncrementAsync("activityResumes");
        if (probe.EnableVariable && context.GetVariable<string>("DurableProof") == "reloaded-variable")
        {
            await probe.IncrementAsync("variableLoadedCorrectly");
        }
        if (probe.ResuspendOnResume)
        {
            context.CreateBookmark(ResumeAsync);
            return;
        }
        context.WorkflowExecutionContext.Output["Proof"] = "persisted-resume-output";
        await context.CompleteActivityAsync();
    }
}
