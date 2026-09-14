using Elsa.Common;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Threading.Tasks;

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
    [Test]
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
        await using var serviceProvider = (IAsyncDisposable)activityExecutionContext.WorkflowExecutionContext.ServiceProvider;
        using var _ = activityExecutionContext;
        
        // Set the properties we want to test
        activityExecutionContext.CallStackDepth = 2;
        activityExecutionContext.SchedulingActivityExecutionId = "context-b";
        activityExecutionContext.SchedulingActivityId = "activity-b";
        
        // Act
        var record = await mapper.MapAsync(activityExecutionContext);
        
        // Assert
        await Assert.That(record).IsNotNull();
        await Assert.That(record.CallStackDepth).IsEqualTo(2);
        await Assert.That(record.SchedulingActivityExecutionId).IsEqualTo("context-b");
        await Assert.That(record.SchedulingActivityId).IsEqualTo("activity-b");
    }

    [Test]
    public async Task MapAsync_IncludesPropertiesAndPayload_WhenInternalStateIsInclude_EvenIfInputsAreExcluded()
    {
        var record = await MapWithPersistenceAsync(
            LogPersistenceMode.Include,
            inputs: LogPersistenceMode.Exclude);

        await Assert.That(record.Properties).IsNotNull();
        await Assert.That(record.Properties["InternalKey"]).IsEqualTo("property-value");
        await Assert.That(record.Payload).IsNotNull();
        await Assert.That(record.Payload["JournalKey"]).IsEqualTo("journal-value");
        await Assert.That(record.ActivityState?.ContainsKey("Text")).IsFalse();
    }

    [Test]
    public async Task MapAsync_ExcludesPropertiesAndPayload_WhenInternalStateIsExclude_EvenIfInputsAreIncluded()
    {
        var record = await MapWithPersistenceAsync(
            LogPersistenceMode.Exclude,
            inputs: LogPersistenceMode.Include);

        await Assert.That(record.Properties).IsNull();
        await Assert.That(record.Payload).IsNull();
        await Assert.That(record.ActivityState?.ContainsKey("Text")).IsTrue();
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
