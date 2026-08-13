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

    private RunWorkflowResult Record(RunWorkflowResult result)
    {
        _result = result;
        _state = result.WorkflowState;
        return result;
    }
}
