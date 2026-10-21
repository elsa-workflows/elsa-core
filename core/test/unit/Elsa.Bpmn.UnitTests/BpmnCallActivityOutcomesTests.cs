using Bpmn.Semantics;
using Elsa.Bpmn.Hosting;
using Elsa.Workflows;

namespace Elsa.Bpmn.UnitTests;

public class BpmnCallActivityOutcomesTests
{
    [Theory(DisplayName = "A faulted or cancelled called workflow reports the interpreter's call-activity failure outcome")]
    [InlineData(WorkflowSubStatus.Faulted, BpmnInterpreter.CallActivityFaultedOutcomeName)]
    [InlineData(WorkflowSubStatus.Cancelled, BpmnInterpreter.CallActivityCancelledOutcomeName)]
    public void FailedChild_ReportsTheFailureOutcome(WorkflowSubStatus childSubStatus, string expectedOutcome)
    {
        Assert.Equal([expectedOutcome], BpmnCallActivityOutcomes.For(childActivationDenied: false, childSubStatus));
    }

    [Theory(DisplayName = "A finished or unknown called workflow reports no outcome, so the call activity's normal flow is taken")]
    [InlineData(WorkflowSubStatus.Finished)]
    [InlineData(null)]
    public void FinishedOrUnknownChild_ReportsNoOutcome(WorkflowSubStatus? childSubStatus)
    {
        Assert.Empty(BpmnCallActivityOutcomes.For(childActivationDenied: false, childSubStatus));
    }

    [Theory(DisplayName = "A called workflow whose activation was denied reports the interpreter's dispatch-failed outcome")]
    [InlineData(null)]
    [InlineData(WorkflowSubStatus.Finished)]
    public void DeniedChild_ReportsTheDispatchFailedOutcome(WorkflowSubStatus? childSubStatus)
    {
        Assert.Equal("DispatchFailed", BpmnInterpreter.CallActivityDispatchFailedOutcomeName);
        Assert.Equal([BpmnInterpreter.CallActivityDispatchFailedOutcomeName], BpmnCallActivityOutcomes.For(childActivationDenied: true, childSubStatus));
    }
}
