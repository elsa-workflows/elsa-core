using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.ActivationValidators;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.ActivationValidators;

public class CorrelatedSingletonStrategyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Throws_WhenCorrelationIdIsBlank(string? correlationId)
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        var strategy = new CorrelatedSingletonStrategy(store);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await strategy.GetAllowActivationAsync(new(CreateWorkflow(), correlationId, CancellationToken.None)));

        await store.DidNotReceiveWithAnyArgs().CountAsync(default!, default);
    }

    [Fact]
    public async Task AllowsActivation_WhenNoRunningInstanceExistsForDefinitionAndCorrelation()
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        store.CountAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns(0);
        var strategy = new CorrelatedSingletonStrategy(store);

        var allowed = await strategy.GetAllowActivationAsync(new(CreateWorkflow("def-1"), "order-1", CancellationToken.None));

        Assert.True(allowed);
        await store.Received(1).CountAsync(
            Arg.Is<WorkflowInstanceFilter>(filter =>
                filter.DefinitionId == "def-1" &&
                filter.CorrelationId == "order-1" &&
                filter.WorkflowStatus == WorkflowStatus.Running),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeniesActivation_WhenRunningInstanceExistsForDefinitionAndCorrelation()
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        store.CountAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns(1);
        var strategy = new CorrelatedSingletonStrategy(store);

        var allowed = await strategy.GetAllowActivationAsync(new(CreateWorkflow(), "order-1", CancellationToken.None));

        Assert.False(allowed);
    }

    private static Workflow CreateWorkflow(string definitionId = "definition-1")
    {
        return new()
        {
            Identity = new WorkflowIdentity(definitionId, 1, definitionId)
        };
    }
}
