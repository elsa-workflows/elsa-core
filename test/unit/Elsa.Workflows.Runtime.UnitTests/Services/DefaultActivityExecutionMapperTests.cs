using Elsa.Common;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Options;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

/// <summary>
/// Unit tests for DefaultActivityExecutionMapper.
/// </summary>
public class DefaultActivityExecutionMapperTests
{
    /// <summary>
    /// Tests that the mapper correctly maps CallStackDepth, SchedulingActivityExecutionId,
    /// and SchedulingActivityId from the ActivityExecutionContext to the record.
    /// </summary>
    [Fact]
    public async Task MapAsync_MapsCallStackDepth_Correctly()
    {
        // Arrange
        var safeSerializer = Substitute.For<ISafeSerializer>();
        safeSerializer.Serialize(Arg.Any<object>()).Returns("serialized");
        
        var payloadSerializer = Substitute.For<IPayloadSerializer>();
        payloadSerializer.Serialize(Arg.Any<object>()).Returns("serialized");
        
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
        
        // Use ActivityTestFixture to create real context objects
        var activity = new WriteLine("Test");
        var fixture = new ActivityTestFixture(activity);
        var activityExecutionContext = await fixture.BuildAsync();
        
        // Set the properties we want to test
        activityExecutionContext.CallStackDepth = 2;
        activityExecutionContext.SchedulingActivityExecutionId = "context-b";
        activityExecutionContext.SchedulingActivityId = "activity-b";
        
        // Act
        var record = await mapper.MapAsync(activityExecutionContext);
        
        // Assert
        Assert.NotNull(record);
        Assert.Equal(2, record.CallStackDepth);
        Assert.Equal("context-b", record.SchedulingActivityExecutionId);
        Assert.Equal("activity-b", record.SchedulingActivityId);
    }
}
