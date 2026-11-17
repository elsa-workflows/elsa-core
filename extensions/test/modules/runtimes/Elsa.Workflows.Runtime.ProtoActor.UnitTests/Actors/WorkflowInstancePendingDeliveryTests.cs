using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime.ProtoActor.UnitTests.Fixtures;
using Elsa.Workflows.State;
using Moq;

namespace Elsa.Workflows.Runtime.ProtoActor.UnitTests.Actors;

public class WorkflowInstancePendingDeliveryTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("different", false)]
    [InlineData("owner", true)]
    public async Task RunsPendingStateOnlyForItsRecordedDelivery(string? deliveryId, bool starts)
    {
        await using var harness = new WorkflowInstanceTestHarness();
        var state = WorkflowInstanceTestHarness.CreateState();
        state.SubStatus = WorkflowSubStatus.Pending;
        state.Properties[WorkflowStartDelivery.IdProperty] = "owner";
        harness.SetupExistingInstance(state);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result = Assert.IsType<RunIfPendingWorkflowInstanceResponse>(
            await harness.RunIfPendingAsync(deliveryId, timeout.Token));

        Assert.Equal(starts, result.Execution != null);
        harness.WorkflowRunner.Verify(x => x.RunAsync(It.IsAny<WorkflowGraph>(), state,
            It.IsAny<RunWorkflowOptions?>(), It.IsAny<CancellationToken>()), starts ? Times.Once() : Times.Never());
    }
}
