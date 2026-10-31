using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;

/// <summary>
/// A stand-in for the decision a gateway binds as work: it finishes as soon as it runs, reporting a fixed set of
/// outcome names, which is what a conditional sequence flow's <c>conditionOutcome</c> is matched against.
/// </summary>
public class BpmnTestDecisionWork : Activity
{
    /// <summary>The log to record into.</summary>
    public BpmnTestLog? Log { get; set; }

    /// <summary>The outcome names the decision reports.</summary>
    public ICollection<string> OutcomeNames { get; set; } = [];

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        BpmnTestWorkProbe.RecordExecution(this, Log, context);
        await context.CompleteActivityAsync(new Outcomes(OutcomeNames.ToArray()));
    }
}
