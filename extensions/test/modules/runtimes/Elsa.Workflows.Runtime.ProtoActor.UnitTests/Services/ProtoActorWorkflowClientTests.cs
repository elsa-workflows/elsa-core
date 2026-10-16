using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime.ProtoActor.Services;
using Moq;

namespace Elsa.Workflows.Runtime.ProtoActor.UnitTests.Services;

public class ProtoActorWorkflowClientTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InstanceExistsAsync_ReturnsStoreResultForWorkflowInstanceId(bool exists)
    {
        const string instanceId = "workflow-instance-1";
        var workflowInstanceManager = new Mock<IWorkflowInstanceManager>();
        workflowInstanceManager
            .Setup(x => x.ExistsAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);
        var client = new ProtoActorWorkflowClient(instanceId, workflowInstanceManager.Object);

        var result = await client.InstanceExistsAsync();

        Assert.Equal(exists, result);
        workflowInstanceManager.Verify(x => x.ExistsAsync(instanceId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
