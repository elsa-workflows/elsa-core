using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.ActivationValidators;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.ActivationValidators;

public class CorrelationStrategyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Throws_WhenCorrelationIdIsBlank(string? correlationId)
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        var strategy = new CorrelationStrategy(store);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await strategy.GetAllowActivationAsync(new(new Workflow(), correlationId, CancellationToken.None)));

        await store.DidNotReceiveWithAnyArgs().CountAsync(default!, default);
    }

    [Fact]
    public async Task AllowsActivation_WhenNoRunningInstanceExistsForCorrelation()
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        store.CountAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns(0);
        var strategy = new CorrelationStrategy(store);

        var allowed = await strategy.GetAllowActivationAsync(new(new Workflow(), "order-1", CancellationToken.None));

        Assert.True(allowed);
        await store.Received(1).CountAsync(
            Arg.Is<WorkflowInstanceFilter>(filter =>
                filter.DefinitionId == null &&
                filter.CorrelationId == "order-1" &&
                filter.WorkflowStatus == WorkflowStatus.Running),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeniesActivation_WhenRunningInstanceExistsForCorrelation()
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        store.CountAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns(1);
        var strategy = new CorrelationStrategy(store);

        var allowed = await strategy.GetAllowActivationAsync(new(new Workflow(), "order-1", CancellationToken.None));

        Assert.False(allowed);
    }
}
