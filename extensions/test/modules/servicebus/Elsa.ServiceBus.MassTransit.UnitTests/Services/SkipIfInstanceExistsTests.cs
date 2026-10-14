using Elsa.ServiceBus.MassTransit.Consumers;
using Elsa.ServiceBus.MassTransit.Contracts;
using Elsa.ServiceBus.MassTransit.Messages;
using Elsa.ServiceBus.MassTransit.Services;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.State;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Elsa.ServiceBus.MassTransit.UnitTests.Services;

/// <summary>
/// Redelivered dispatches with a fixed instance ID (for example an outbox retry) must not create or reset a second instance (#8811).
/// </summary>
public class SkipIfInstanceExistsTests
{
    private const string InstanceId = "child-1";
    private const string DefinitionVersionId = "version-1";

    private readonly Mock<IWorkflowInstanceManager> _instanceManager = new();
    private readonly Mock<IWorkflowDefinitionService> _definitionService = new();
    private readonly Mock<ISendEndpoint> _sendEndpoint = new();
    private readonly Mock<IBus> _bus = new();
    private readonly Mock<IWorkflowRuntime> _runtime = new();
    private readonly Mock<IWorkflowClient> _client = new();

    public SkipIfInstanceExistsTests()
    {
        var workflow = new Workflow { Id = "definition-1", Identity = new WorkflowIdentity("definition-1", 1, DefinitionVersionId) };
        var node = new ActivityNode(workflow, "root");
        var graph = new WorkflowGraph(workflow, node, [node]);
        _definitionService.Setup(x => x.FindWorkflowGraphAsync(DefinitionVersionId, It.IsAny<CancellationToken>())).ReturnsAsync(graph);
        _instanceManager.Setup(x => x.CreateAndCommitWorkflowInstanceAsync(It.IsAny<Workflow>(), It.IsAny<WorkflowInstanceOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Elsa.Workflows.Management.Entities.WorkflowInstance { Id = InstanceId, WorkflowState = new WorkflowState() });
        _bus.Setup(x => x.GetSendEndpoint(It.IsAny<Uri>())).ReturnsAsync(_sendEndpoint.Object);
        _runtime.Setup(x => x.CreateClientAsync(InstanceId, It.IsAny<CancellationToken>())).ReturnsAsync(_client.Object);
    }

    [Fact]
    public async Task DispatcherSkipsWhenFlagIsSetAndInstanceExists()
    {
        _instanceManager.Setup(x => x.ExistsAsync(InstanceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var response = await CreateDispatcher().DispatchAsync(CreateRequest(skipIfInstanceExists: true));

        Assert.True(response.Succeeded);
        _instanceManager.Verify(x => x.CreateAndCommitWorkflowInstanceAsync(It.IsAny<Workflow>(), It.IsAny<WorkflowInstanceOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
        _sendEndpoint.Verify(x => x.Send(It.IsAny<DispatchWorkflowDefinition>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task DispatcherCreatesAndSendsOtherwise(bool skipIfInstanceExists, bool instanceExists)
    {
        _instanceManager.Setup(x => x.ExistsAsync(InstanceId, It.IsAny<CancellationToken>())).ReturnsAsync(instanceExists);

        await CreateDispatcher().DispatchAsync(CreateRequest(skipIfInstanceExists));

        _instanceManager.Verify(x => x.CreateAndCommitWorkflowInstanceAsync(It.IsAny<Workflow>(), It.Is<WorkflowInstanceOptions?>(o => o!.WorkflowInstanceId == InstanceId), It.IsAny<CancellationToken>()), Times.Once);
        _sendEndpoint.Verify(x => x.Send(It.Is<DispatchWorkflowDefinition>(m => m.InstanceId == InstanceId && m.IsExistingInstance), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConsumerSkipsNewInstanceWhenFlagIsSetAndInstanceExists()
    {
        _client.Setup(x => x.InstanceExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await CreateConsumer().Consume(CreateContext(skipIfInstanceExists: true));

        _client.Verify(x => x.CreateAndRunInstanceAsync(It.IsAny<CreateAndRunWorkflowInstanceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ConsumerCreatesNewInstanceOtherwise(bool skipIfInstanceExists, bool instanceExists)
    {
        _client.Setup(x => x.InstanceExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(instanceExists);

        await CreateConsumer().Consume(CreateContext(skipIfInstanceExists));

        _client.Verify(x => x.CreateAndRunInstanceAsync(It.IsAny<CreateAndRunWorkflowInstanceRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private MassTransitWorkflowDispatcher CreateDispatcher()
    {
        var formatter = new Mock<IEndpointChannelFormatter>();
        formatter.Setup(x => x.FormatEndpointName(It.IsAny<string?>())).Returns("dispatch");
        return new(_bus.Object, formatter.Object, _definitionService.Object, _instanceManager.Object, Mock.Of<IStimulusHasher>(), Mock.Of<ITriggerStore>(), Mock.Of<IBookmarkStore>(), NullLogger<MassTransitWorkflowDispatcher>.Instance);
    }

    private DispatchWorkflowRequestConsumer CreateConsumer() => new(_definitionService.Object, _runtime.Object);

    private static DispatchWorkflowDefinitionRequest CreateRequest(bool skipIfInstanceExists) =>
        new(DefinitionVersionId) { InstanceId = InstanceId, SkipIfInstanceExists = skipIfInstanceExists };

    private static ConsumeContext<DispatchWorkflowDefinition> CreateContext(bool skipIfInstanceExists)
    {
        var context = new Mock<ConsumeContext<DispatchWorkflowDefinition>>();
        context.Setup(x => x.Message).Returns(new DispatchWorkflowDefinition { DefinitionVersionId = DefinitionVersionId, InstanceId = InstanceId, SkipIfInstanceExists = skipIfInstanceExists });
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        return context.Object;
    }
}
