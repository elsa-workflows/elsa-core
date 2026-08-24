using Bpmn.Semantics;
using Elsa.Workflows;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

/// <summary>
/// Event subprocesses, both flavours, and the three things about them that are the host's to get right: a listener
/// armed at scope start and retired when the scope completes, the start-element hint reaching the body, and a
/// re-armed non-interrupting listener not colliding with the slot the fire that armed it just vacated.
/// </summary>
/// <remarks>
/// <para>
/// A dormant catcher — error or escalation — needs nothing armed: it rides the <c>FaultSignal</c> seam and the
/// escalation signal path, and what arrives at the host is an ordinary <c>StartWork</c> for the body. A
/// listener-backed catcher — message, signal or timer — additionally gets a <c>StartWork</c> for its
/// <c>listenerBindingRef</c> at scope start and a <c>CancelWorkSubtree</c> for it when the scope completes. Both are
/// commands the applier already handles, so these are process-level tests rather than tests of an
/// event-subprocess-specific code path: there is none.
/// </para>
/// <para>
/// Every process runs under <see cref="FaultStrategy"/>. An event subprocess's failure mode is a quiet one — a body
/// that was never seeded, a listener that was never armed, a catcher that never fired — and all three leave a
/// workflow that finished and reported nothing. Faulting rather than absorbing into an incident keeps a refusal this
/// host cannot honour from being buried under work that carried on regardless.
/// </para>
/// </remarks>
public class BpmnEventSubprocessTests(ITestOutputHelper testOutputHelper)
{
    private readonly BpmnTestHost _host = new(testOutputHelper);

    [Fact(DisplayName = "A dormant error-triggered event subprocess catches a fault in its scope and routes to its body")]
    public async Task ErrorEventSubprocess_CatchesTheFaultAndRunsItsBody()
    {
        // The whole log, not a Contains: "the body ran" is also true of a process that carried on down the sequence
        // flow afterwards, and 'after' is exactly the work an error the scope only appeared to claim would reach.
        // 'cancelled:risky' is in it deliberately -- a scope claiming a fault terminalizes the whole unit of work
        // that failed, and that teardown is the host's own doing rather than a command the interpreter issued.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.ErrorEventSubprocess(_host.Log), typeof(FaultStrategy));

        // Assert
        Assert.Equal(["executed:risky", "cancelled:risky", "executed:handleError"], _host.Log.Entries);

        // The disposition was Caught, so the scope claimed the fault and terminalized the failed work itself.
        Assert.Equal(ActivityStatus.Canceled, StatusOf(result, "risky"));

        // And a fault a container claimed is not an incident.
        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A dormant escalation-triggered event subprocess catches an escalation raised in a nested scope")]
    public async Task EscalationEventSubprocess_CatchesTheEscalationOutOfTheNestedScope()
    {
        // The scope-level catcher, not a boundary event on the subprocess: the escalation crosses the scope boundary
        // on the same signal path, and what claims it is an event subprocess whose body start event declares the
        // matching code. Non-interrupting, so the subprocess that escalated keeps running -- which is what tells
        // "the catcher fired" apart from "the subprocess was stopped".

        // Arrange
        await _host.RunAsync(BpmnTestProcesses.EscalationEventSubprocessOutOfSubprocess(_host.Log), typeof(FaultStrategy));

        // Act: the subprocess reaches its escalation throw event.
        await _host.FinishWorkAsync("subWork");

        // Assert: the event subprocess body ran...
        Assert.Contains("executed:handleEscalation", _host.Log.Entries);

        // ...and the escalating subprocess carried on past the throw rather than being torn down.
        Assert.Contains("executed:subMore", _host.Log.Entries);
        Assert.DoesNotContain("cancelled:subMore", _host.Log.Entries);

        // And the subprocess still completes normally, so the main path continues.
        var result = await _host.FinishWorkAsync("subMore");

        Assert.Contains("executed:after", _host.Log.Entries);
        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A message-triggered event subprocess arms its listener at scope start and runs its body when the trigger fires")]
    public async Task MessageEventSubprocess_ArmsItsListenerAtScopeStartAndRunsItsBodyWhenFired()
    {
        // Armed *at scope start* is the claim, so it is asserted before the trigger fires and not inferred from the
        // fire having worked. The scope's own ledger -- the sole source of BpmnHostSnapshot.LiveWork -- already holds
        // the listener alongside the scope's ordinary work by the time that work runs.

        // Arrange
        await _host.RunAsync(BpmnTestProcesses.MessageEventSubprocess(_host.Log), typeof(FaultStrategy));

        // Assert: armed, and nothing has fired.
        Assert.Equal(
            [BpmnTestProcesses.BindingRef("nudgeListener"), BpmnTestProcesses.BindingRef("work")],
            _host.Log.Snapshot("liveWork@work"));

        Assert.DoesNotContain("executed:handleNudge", _host.Log.Entries);

        // Act: the trigger fires while the scope's own work is still running.
        await _host.FinishWorkAsync("nudgeListener");

        // Assert: the body ran, and the scope's own work is untouched -- non-interrupting means exactly that.
        Assert.Equal(1, _host.Log.Occurrences("executed:handleNudge"));
        Assert.DoesNotContain("cancelled:work", _host.Log.Entries);

        var result = await _host.FinishWorkAsync("work");

        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A scope completing with its listener still armed retires it, and the armed work does not survive")]
    public async Task MessageEventSubprocess_RetiresTheStillArmedListenerWhenTheScopeCompletes()
    {
        // The quiet failure this pins: a listener left armed is a live child activity holding a bookmark on a scope
        // that is already finished. The workflow reports Finished either way -- what differs is whether anything can
        // still resume into a scope that has completed, which is a process that would run its event subprocess body
        // after the process containing it ended.

        // Arrange
        await _host.RunAsync(BpmnTestProcesses.MessageEventSubprocess(_host.Log), typeof(FaultStrategy));

        Assert.Contains(BpmnTestProcesses.BindingRef("nudgeListener"), _host.LiveWorkOf("scope").Select(work => work.BindingRef));

        // Act: the scope's own work completes, which is the last thing keeping the scope open.
        var result = await _host.FinishWorkAsync("work");

        // Assert: the listener was torn down, never fired, and left nothing live behind it.
        //
        // The ledger assertion is the one that carries the weight here. At the root, Elsa tears a finished workflow's
        // remaining children down on its own, so 'cancelled:nudgeListener' and the empty bookmark set would both hold
        // even if the retirement command were dropped on the floor; what would not is the scope's own record of what
        // it still has running. NestedMessageEventSubprocess_... below is the same retirement where the workflow
        // outlives the scope, which is where the rest of it stops being free.
        Assert.Contains("cancelled:nudgeListener", _host.Log.Entries);
        Assert.DoesNotContain("executed:handleNudge", _host.Log.Entries);
        Assert.Empty(_host.LiveWorkOf("scope"));
        Assert.Empty(result.WorkflowState.Bookmarks);

        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A nested scope completing with its listener still armed retires it while the workflow carries on")]
    public async Task NestedMessageEventSubprocess_RetiresTheStillArmedListenerWhenTheNestedScopeCompletes()
    {
        // The same retirement, in the only shape where it is the scope's doing rather than the workflow's: the
        // enclosing process keeps running afterwards, so a listener the scope failed to retire is one that outlives
        // it and could still be resumed into a subprocess that has already finished.

        // Arrange
        await _host.RunAsync(BpmnTestProcesses.NestedMessageEventSubprocess(_host.Log), typeof(FaultStrategy));

        Assert.Contains(BpmnTestProcesses.BindingRef("nudgeListener"), _host.LiveWorkOf("sub").Select(work => work.BindingRef));

        // Act: the subprocess's own work completes, which is the last thing keeping the nested scope open.
        var result = await _host.FinishWorkAsync("subWork");

        // Assert: the nested scope retired its listener and completed, and the enclosing scope carried on.
        Assert.Contains("cancelled:nudgeListener", _host.Log.Entries);
        Assert.DoesNotContain("executed:handleNudge", _host.Log.Entries);
        Assert.Empty(_host.LiveWorkOf("sub"));
        Assert.Contains("executed:after", _host.Log.Entries);

        // Nothing is left for a stimulus to resume into.
        Assert.Empty(result.WorkflowState.Bookmarks);

        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A re-armed non-interrupting listener fires again cleanly, holding one live slot at a time")]
    public async Task NonInterruptingListener_ReArmsWithoutCollidingWithTheSlotItJustVacated()
    {
        // The interpreter re-finds a parked token from (binding ref, iteration id) alone, and a re-armed listener
        // keys onto the very slot the fire that armed it just vacated. It is only free because the completing
        // listener was removed from the ledger before the interpreter was asked; leaving it there gives the scope two
        // live records and two bookmarks for one slot, and the next teardown or completion resolves to whichever one
        // it happens to find first.
        //
        // Firing twice rather than once is what makes that visible: the first fire is indistinguishable either way.

        // Arrange
        await _host.RunAsync(BpmnTestProcesses.MessageEventSubprocess(_host.Log), typeof(FaultStrategy));

        // Act: fire once...
        await _host.FinishWorkAsync("nudgeListener");

        // Assert: the body ran, and exactly one listener is armed -- not the fresh one alongside the finished one.
        Assert.Equal(1, _host.Log.Occurrences("executed:handleNudge"));
        Assert.Equal(1, LiveListenerRecords());

        // Act: ...and again, onto the slot the first fire vacated.
        await _host.FinishWorkAsync("nudgeListener");

        // Assert: a second, complete run of the body, and still exactly one armed listener.
        Assert.Equal(2, _host.Log.Occurrences("executed:handleNudge"));
        Assert.Equal(1, LiveListenerRecords());

        var result = await _host.FinishWorkAsync("work");

        // The scope retires that last listener and finishes, which a scope holding a stale second record could not do
        // cleanly: the teardown would resolve the wrong record and leave the other bookmark behind.
        Assert.Contains("cancelled:nudgeListener", _host.Log.Entries);
        Assert.Empty(result.WorkflowState.Bookmarks);
        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "An event subprocess body is seeded at the start event named on the hint, and nothing else inherits it")]
    public async Task EventSubprocessBody_IsSeededAtTheHintedStartEvent()
    {
        // Both directions of the hint, in one process, and each fails loudly rather than quietly.
        //
        // The body's only start event is event-defined, so a body that did not receive the hint has nothing to begin
        // at: it faults with bpmn.start.none-available rather than starting anywhere plausible. The ordinary
        // subprocess inside the body is the other direction -- its own invocation carries an ordinary scheduling
        // cause, so inheriting the hint would seed it at an element it does not declare and fault it with
        // bpmn.start.unresolved-hint.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.EventSubprocessBodyWithNestedSubprocess(_host.Log), typeof(FaultStrategy));

        // Assert
        Assert.Equal(["executed:risky", "cancelled:risky", "executed:handleError", "executed:innerOnly"], _host.Log.Entries);
        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);

        // The dictionary the hint arrives in belongs to the scope and is fixed for its lifetime. Read after the body
        // has started and finished work of its own, it still says what the StartWork that created the scope said: a
        // host that wrote a started or completing unit of work's correlation here would have overwritten it, and the
        // damage would only surface the next time a body was seeded.
        var correlation = _host.InvocationCorrelationOf("evtSub");

        Assert.Equal("errStart", correlation[BpmnInterpreter.StartElementIdCorrelationKey]);
        Assert.Equal(BpmnInterpreter.EventSubprocessBodySchedulingCause, correlation[BpmnInterpreter.SchedulingCauseCorrelationKey]);
    }

    [Fact(DisplayName = "An event subprocess body declaring more than one start event is refused")]
    public Task EventSubprocessBodyWithTwoStartEvents_IsRefused() =>
        AssertRefusedAsync(BpmnTestProcesses.EventSubprocessBodyWithTwoStartEvents(_host.Log), "body must declare exactly one start event");

    [Fact(DisplayName = "A second error-triggered event subprocess in one scope is refused")]
    public Task TwoErrorEventSubprocesses_AreRefused() =>
        AssertRefusedAsync(BpmnTestProcesses.TwoErrorEventSubprocesses(_host.Log), "more than one error event subprocess");

    [Fact(DisplayName = "A second code-less catch-all escalation-triggered event subprocess in one scope is refused")]
    public Task TwoCatchAllEscalationEventSubprocesses_AreRefused() =>
        AssertRefusedAsync(
            BpmnTestProcesses.TwoCatchAllEscalationEventSubprocesses(_host.Log),
            "more than one code-less catch-all escalation event subprocess");

    [Fact(DisplayName = "A non-interrupting error-triggered event subprocess is refused")]
    public Task NonInterruptingErrorEventSubprocess_IsRefused() =>
        AssertRefusedAsync(BpmnTestProcesses.NonInterruptingErrorEventSubprocess(_host.Log), "must be interrupting");

    /// <summary>
    /// Asserts a process the library refuses is refused, and refused before anything ran.
    /// </summary>
    /// <remarks>
    /// These are the library's rules about how an event subprocess may be declared, and it enforces them when the
    /// scope builds its graph — before a single unit of work is started. Pinned here so a future reader meets them as
    /// refusals rather than as gaps: the process does not half-run and then stop, it never starts.
    /// </remarks>
    private async Task AssertRefusedAsync(IActivity process, string expectedMessageFragment)
    {
        var result = await _host.RunAsync(process, typeof(FaultStrategy));

        Assert.Empty(_host.Log.Entries);
        Assert.Equal(WorkflowSubStatus.Faulted, result.WorkflowState.SubStatus);

        var incident = Assert.Single(result.WorkflowState.Incidents);

        Assert.Contains(expectedMessageFragment, incident.Exception!.Message);
    }

    private int LiveListenerRecords() =>
        _host.LiveWorkOf("scope").Count(record => record.BindingRef == BpmnTestProcesses.BindingRef("nudgeListener"));

    private static ActivityStatus? StatusOf(RunWorkflowResult result, string activityId) =>
        result.Journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity.Id == activityId)?.Status;
}
