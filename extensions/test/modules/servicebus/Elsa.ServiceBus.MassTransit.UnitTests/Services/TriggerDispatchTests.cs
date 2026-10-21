using Elsa.ServiceBus.MassTransit.Contracts;
using Elsa.ServiceBus.MassTransit.Messages;
using Elsa.ServiceBus.MassTransit.Services;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.State;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Elsa.ServiceBus.MassTransit.UnitTests.Services;

/// <summary>
/// A trigger dispatch creates the instance up front, so the matched trigger's hash has to be on the instance it creates.
/// </summary>
public class TriggerDispatchTests
{
    private const string DefinitionVersionId = "version-1";
    private const string TriggerHash = "trigger-hash";

    [Fact]
    public async Task DispatchingTriggers_CreatesTheInstanceWithTheMatchedTriggersHashInACopyOfTheProperties()
    {
        var workflow = new Workflow { Id = "definition-1", Identity = new WorkflowIdentity("definition-1", 1, DefinitionVersionId) };
        var node = new ActivityNode(workflow, "root");
        var definitionService = new Mock<IWorkflowDefinitionService>();
        definitionService.Setup(x => x.FindWorkflowGraphAsync(DefinitionVersionId, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkflowGraph(workflow, node, [node]));
        var triggerStore = new Mock<ITriggerStore>();
        triggerStore.Setup(x => x.FindManyAsync(It.IsAny<TriggerFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new StoredTrigger { Id = "trigger-1", WorkflowDefinitionVersionId = DefinitionVersionId, ActivityId = "start", Hash = TriggerHash }]);
        var instanceManager = new Mock<IWorkflowInstanceManager>();
        WorkflowInstanceOptions? createdWith = null;
        instanceManager.Setup(x => x.CreateAndCommitWorkflowInstanceAsync(It.IsAny<Workflow>(), It.IsAny<WorkflowInstanceOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<Workflow, WorkflowInstanceOptions?, CancellationToken>((_, options, _) => createdWith = options)
            .ReturnsAsync(new WorkflowInstance { Id = "instance-1", WorkflowState = new WorkflowState() });
        var sendEndpoint = new Mock<ISendEndpoint>();
        var bus = new Mock<IBus>();
        bus.Setup(x => x.GetSendEndpoint(It.IsAny<Uri>())).ReturnsAsync(sendEndpoint.Object);
        var formatter = new Mock<IEndpointChannelFormatter>();
        formatter.Setup(x => x.FormatEndpointName(It.IsAny<string?>())).Returns("dispatch");
        var dispatcher = new MassTransitWorkflowDispatcher(
            bus.Object, formatter.Object, definitionService.Object, instanceManager.Object, Mock.Of<IStimulusHasher>(), triggerStore.Object, Mock.Of<IBookmarkStore>(),
            NullLogger<MassTransitWorkflowDispatcher>.Instance);
        var callerProperties = new Dictionary<string, object> { ["Caller"] = "value" };

        await dispatcher.DispatchAsync(new DispatchTriggerWorkflowsRequest("Elsa.Event", new object()) { Properties = callerProperties });

        Assert.NotNull(createdWith?.Properties);
        Assert.Equal(TriggerHash, createdWith.Properties[TriggerWorkflowProperties.StimulusHash]);
        Assert.Equal("value", createdWith.Properties["Caller"]);
        Assert.False(callerProperties.ContainsKey(TriggerWorkflowProperties.StimulusHash));
        sendEndpoint.Verify(x => x.Send(It.Is<DispatchWorkflowDefinition>(m => m.IsExistingInstance && m.TriggerActivityId == "start"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
