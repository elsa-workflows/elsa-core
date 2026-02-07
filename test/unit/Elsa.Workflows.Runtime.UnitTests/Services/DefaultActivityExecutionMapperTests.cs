using Elsa.Common;
using Elsa.Common.Codecs;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.State;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class DefaultActivityExecutionMapperTests
{
    [Fact]
    public async Task MapAsync_MapsCallStackDepth_Correctly()
    {
        // Arrange
        var safeSerializer = Substitute.For<ISafeSerializer>();
        var payloadSerializer = Substitute.For<IPayloadSerializer>();
        var compressionCodecResolver = Substitute.For<ICompressionCodecResolver>();
        var compressionCodec = Substitute.For<ICompressionCodec>();
        compressionCodecResolver.Resolve(Arg.Any<string>()).Returns(compressionCodec);
        compressionCodec.CompressAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new ValueTask<string>("compressed"));
        
        var managementOptions = Substitute.For<IOptions<ManagementOptions>>();
        managementOptions.Value.Returns(new ManagementOptions());
        
        var mapper = new DefaultActivityExecutionMapper(
            safeSerializer,
            payloadSerializer,
            compressionCodecResolver,
            managementOptions);
        
        var activity = new WriteLine("Test");
        var workflowExecutionContext = Substitute.For<WorkflowExecutionContext>();
        workflowExecutionContext.Id.Returns("workflow-123");
        
        var activityExecutionContext = Substitute.For<ActivityExecutionContext>();
        activityExecutionContext.Id.Returns("context-c");
        activityExecutionContext.Activity.Returns(activity);
        activityExecutionContext.NodeId.Returns("node-c");
        activityExecutionContext.WorkflowExecutionContext.Returns(workflowExecutionContext);
        activityExecutionContext.Status.Returns(ActivityStatus.Running);
        activityExecutionContext.CallStackDepth.Returns(2);
        activityExecutionContext.SchedulingActivityExecutionId.Returns("context-b");
        activityExecutionContext.SchedulingActivityId.Returns("activity-b");
        activityExecutionContext.CancellationToken.Returns(CancellationToken.None);
        
        // Act
        var record = await mapper.MapAsync(activityExecutionContext);
        
        // Assert
        Assert.Equal(2, record.CallStackDepth);
        Assert.Equal("context-b", record.SchedulingActivityExecutionId);
        Assert.Equal("activity-b", record.SchedulingActivityId);
    }
}
