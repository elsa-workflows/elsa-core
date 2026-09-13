using Elsa.Common;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Runtime.Entities;
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

    [Fact]
    public async Task MapAsync_IncludesPropertiesAndPayload_WhenInternalStateIsInclude_EvenIfInputsAreExcluded()
    {
        var record = await MapWithPersistenceAsync(
            LogPersistenceMode.Include,
            inputs: LogPersistenceMode.Exclude);

        Assert.NotNull(record.Properties);
        Assert.Equal("property-value", record.Properties["InternalKey"]);
        Assert.NotNull(record.Payload);
        Assert.Equal("journal-value", record.Payload["JournalKey"]);
        Assert.False(record.ActivityState?.ContainsKey("Text"));
    }

    [Fact]
    public async Task MapAsync_ExcludesPropertiesAndPayload_WhenInternalStateIsExclude_EvenIfInputsAreIncluded()
    {
        var record = await MapWithPersistenceAsync(
            LogPersistenceMode.Exclude,
            inputs: LogPersistenceMode.Include);

        Assert.Null(record.Properties);
        Assert.Null(record.Payload);
        Assert.True(record.ActivityState?.ContainsKey("Text"));
    }

    private static async Task<ActivityExecutionRecord> MapWithPersistenceAsync(
        LogPersistenceMode internalState,
        LogPersistenceMode inputs)
    {
        var mapper = CreateMapper();
        var activity = new WriteLine("Test");
        var context = await new ActivityTestFixture(activity).BuildAsync();

        context.ActivityState["Text"] = "Test";
        context.Properties["InternalKey"] = "property-value";
        context.JournalData["JournalKey"] = "journal-value";
        context.SetLogPersistenceModeMap(new ActivityLogPersistenceModeMap
        {
            InternalState = internalState,
            Inputs = { [nameof(WriteLine.Text)] = inputs }
        });

        return await mapper.MapAsync(context);
    }

    private static DefaultActivityExecutionMapper CreateMapper()
    {
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

        return new DefaultActivityExecutionMapper(
            safeSerializer,
            payloadSerializer,
            compressionCodecResolver,
            managementOptions);
    }
}
