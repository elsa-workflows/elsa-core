using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Event;

public class PublishEventTests
{
    [Fact]
    public async Task ExecuteAsync_PublishesEvent_WithEventName()
    {
        // Arrange
        const string eventName = "OrderCreated";
        var activity = new PublishEvent
        {
            EventName = new(eventName)
        };
        var publisher = Substitute.For<IEventPublisher>();

        // Act
        await ExecuteAsync(activity, publisher);

        // Assert
        await publisher.Received(1).PublishAsync(
            eventName,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<object?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PublishesEvent_WithCorrelationId()
    {
        // Arrange
        const string eventName = "OrderCreated";
        const string correlationId = "correlation-123";
        var activity = new PublishEvent
        {
            EventName = new(eventName),
            CorrelationId = new(correlationId)
        };
        var publisher = Substitute.For<IEventPublisher>();

        // Act
        await ExecuteAsync(activity, publisher);

        // Assert
        await publisher.Received(1).PublishAsync(
            eventName,
            correlationId,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<object?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PublishesEvent_WithPayload()
    {
        // Arrange
        const string eventName = "OrderCreated";
        var payload = new { OrderId = 123, Amount = 99.99m };
        var activity = new PublishEvent
        {
            EventName = new(eventName),
            Payload = new(payload)
        };
        var publisher = Substitute.For<IEventPublisher>();

        // Act
        await ExecuteAsync(activity, publisher);

        // Assert
        await publisher.Received(1).PublishAsync(
            eventName,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            payload,
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_LocalEvent_PassesWorkflowInstanceId()
    {
        // Arrange
        const string eventName = "LocalOrderEvent";
        var activity = new PublishEvent
        {
            EventName = new(eventName),
            IsLocalEvent = new(true)
        };
        var publisher = Substitute.For<IEventPublisher>();

        // Act
        var context = await ExecuteAsync(activity, publisher);

        // Assert
        await publisher.Received(1).PublishAsync(
            eventName,
            Arg.Any<string?>(),
            context.WorkflowExecutionContext.Id,
            Arg.Any<string?>(),
            Arg.Any<object?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_NonLocalEvent_PassesNullWorkflowInstanceId()
    {
        // Arrange
        const string eventName = "GlobalOrderEvent";
        var activity = new PublishEvent
        {
            EventName = new(eventName),
            IsLocalEvent = new(false)
        };
        var publisher = Substitute.For<IEventPublisher>();

        // Act
        await ExecuteAsync(activity, publisher);

        // Assert
        await publisher.Received(1).PublishAsync(
            eventName,
            Arg.Any<string?>(),
            null,
            Arg.Any<string?>(),
            Arg.Any<object?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_EmptyCorrelationId_PassesNullCorrelationId(string emptyCorrelationId)
    {
        // Arrange
        const string eventName = "OrderEvent";
        var activity = new PublishEvent
        {
            EventName = new(eventName),
            CorrelationId = new(emptyCorrelationId)
        };
        var publisher = Substitute.For<IEventPublisher>();

        // Act
        await ExecuteAsync(activity, publisher);

        // Assert
        await publisher.Received(1).PublishAsync(
            eventName,
            null,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<object?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_CompletesActivity()
    {
        // Arrange
        var activity = new PublishEvent
        {
            EventName = new("TestEvent")
        };
        var publisher = Substitute.For<IEventPublisher>();

        // Act
        var context = await ExecuteAsync(activity, publisher);

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithAllParameters_PassesAllValuesToPublisher()
    {
        // Arrange
        const string eventName = "CompleteOrderEvent";
        const string correlationId = "correlation-456";
        var payload = new { Status = "Shipped" };
        var activity = new PublishEvent
        {
            EventName = new(eventName),
            CorrelationId = new(correlationId),
            IsLocalEvent = new(true),
            Payload = new(payload)
        };
        var publisher = Substitute.For<IEventPublisher>();

        // Act
        var context = await ExecuteAsync(activity, publisher);

        // Assert
        await publisher.Received(1).PublishAsync(
            eventName,
            correlationId,
            context.WorkflowExecutionContext.Id,
            null,
            payload,
            true,
            Arg.Any<CancellationToken>());
    }

    private static async Task<ActivityExecutionContext> ExecuteAsync(PublishEvent activity, IEventPublisher publisher)
    {
        var fixture = new ActivityTestFixture(activity)
            .ConfigureServices(services => services.AddSingleton(publisher));

        return await fixture.ExecuteAsync();
    }
}
