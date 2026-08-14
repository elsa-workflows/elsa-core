using System.Text.Json;
using Bpmn.Model.State;
using Elsa.Bpmn.Hosting;
using Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

/// <summary>
/// Runs a BPMN scope through <see cref="IWorkflowRunner"/> and lets a test finish blocked work by activity id.
/// </summary>
public sealed class BpmnTestHost
{
    private readonly IServiceProvider _services;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;

    private Workflow? _workflow;
    private WorkflowState? _state;
    private RunWorkflowResult? _result;

    public BpmnTestHost(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseBpmn())
            .AddActivitiesFrom<BpmnTestWork>()
            .Build();

        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
        _workflowBuilderFactory = _services.GetRequiredService<IWorkflowBuilderFactory>();
    }

    /// <summary>What the stand-in work activities recorded.</summary>
    public BpmnTestLog Log { get; } = new();

    /// <summary>Runs the given root activity to quiescence.</summary>
    public async Task<RunWorkflowResult> RunAsync(IActivity root, Type? incidentStrategyType = null)
    {
        await _services.PopulateRegistriesAsync();

        _workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync(new TestWorkflow(builder =>
        {
            builder.WorkflowOptions.IncidentStrategyType = incidentStrategyType;
            builder.Root = root;
        }));

        return Record(await _workflowRunner.RunAsync(_workflow));
    }

    /// <summary>
    /// Finishes the blocked work of the named activity, which is how a test fires a timer, delivers a message, or
    /// completes a long-running task.
    /// </summary>
    public async Task<RunWorkflowResult> FinishWorkAsync(string activityId)
    {
        var state = _state ?? throw new InvalidOperationException("The workflow has not been run yet.");

        var bookmark = state.Bookmarks.FirstOrDefault(x => x.ActivityId == activityId)
                       ?? throw new InvalidOperationException(
                           $"Activity '{activityId}' is not blocked. Blocked activities: {(state.Bookmarks.Count == 0 ? "(none)" : string.Join(", ", state.Bookmarks.Select(x => x.ActivityId)))}.");

        return Record(await _workflowRunner.RunAsync(_workflow!, state, new RunWorkflowOptions
        {
            BookmarkId = bookmark.Id
        }));
    }

    /// <summary>
    /// The work the named BPMN scope currently reports as live, read from its own persisted ledger — which is the sole
    /// source of <c>BpmnHostSnapshot.LiveWork</c>.
    /// </summary>
    public IReadOnlyList<(string BindingRef, string? IterationId)> LiveWorkOf(string scopeActivityId)
    {
        var scopeContext = _result!.Journal.ActivityExecutionContexts.First(x => x.Activity.Id == scopeActivityId);
        var ledger = BpmnScopeMemory.Read<BpmnWorkLedger>(scopeContext, BpmnScopeMemory.WorkLedgerPropertyKey) ?? new BpmnWorkLedger();

        return ledger.Records.Select(record => (record.BindingRef, record.IterationId)).ToList();
    }

    /// <summary>
    /// Replaces the current <see cref="WorkflowState"/> with what a round trip through Elsa's own
    /// <see cref="IWorkflowStateSerializer"/> hands back — what a real persistence store would return on load,
    /// rather than the exact same in-memory object the previous run produced.
    /// </summary>
    public void RoundTripStateThroughJson()
    {
        var state = _state ?? throw new InvalidOperationException("The workflow has not been run yet.");
        var serializer = _services.GetRequiredService<IWorkflowStateSerializer>();

        _state = serializer.Deserialize(serializer.Serialize(state));
    }

    /// <summary>
    /// The one BPMN scope's own memory, read straight off the current <see cref="WorkflowState"/> the way a
    /// resumed scope itself sees it, rather than off any live <see cref="ActivityExecutionContext"/> the current
    /// process happens to still hold. Only sound for a process with exactly one BPMN scope.
    /// </summary>
    internal (BpmnExecutionState? State, BpmnWorkLedger Work) PersistedScopeMemory()
    {
        var state = _state ?? throw new InvalidOperationException("The workflow has not been run yet.");
        var scopeState = state.ActivityExecutionContexts.Single(x => x.Properties.ContainsKey(BpmnScopeMemory.WorkLedgerPropertyKey));

        var executionStateJson = scopeState.Properties.TryGetValue(BpmnScopeMemory.ExecutionStatePropertyKey, out var value) ? value as string : null;
        var workLedgerJson = (string)scopeState.Properties[BpmnScopeMemory.WorkLedgerPropertyKey];

        return (
            executionStateJson is null ? null : JsonSerializer.Deserialize<BpmnExecutionState>(executionStateJson),
            JsonSerializer.Deserialize<BpmnWorkLedger>(workLedgerJson) ?? new BpmnWorkLedger());
    }

    private RunWorkflowResult Record(RunWorkflowResult result)
    {
        _result = result;
        _state = result.WorkflowState;
        return result;
    }
}
