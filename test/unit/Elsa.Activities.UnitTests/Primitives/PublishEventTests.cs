using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Primitives;

public class PublishEventTests
{
    [Theory]
    [InlineData("OrderCreated", null, null, false)]
    [InlineData("OrderCreated", "correlation-123", "correlation-123", false)]
    [InlineData("OrderEvent", "", null, true)]
    [InlineData("OrderEvent", "   ", null, true)]
    public async Task ExecuteAsync_PublishesEvent_WithParameters(string eventName, string? correlationId, string? expectedCorrelationId, bool expectNullCorrelation)
    {
        // Arrange
        var publisher = Substitute.For<IEventPublisher>();

        // Act
        await ExecuteAsync(CreateActivity(eventName, correlationId), publisher);

        // Assert
        await AssertPublishedAsync(
            publisher,
            eventName,
            correlationId: expectedCorrelationId,
            expectNullCorrelationId: expectNullCorrelation);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ExecuteAsync_LocalEvent_PassesCorrectWorkflowInstanceId(bool isLocalEvent, bool expectNull)
    {
        // Arrange
        const string eventName = "TestEvent";
        var publisher = Substitute.For<IEventPublisher>();

        // Act
        var context = await ExecuteAsync(CreateActivity(eventName, isLocalEvent: isLocalEvent), publisher);

        // Assert
        await AssertPublishedAsync(
            publisher,
            eventName,
            workflowInstanceId: expectNull ? null : context.WorkflowExecutionContext.Id,
            expectNullWorkflowInstanceId: expectNull);
    }

    [Fact]
    public async Task ExecuteAsync_PublishesEvent_WithPayload()
    {
        // Arrange
        const string eventName = "OrderCreated";
        var payload = new { OrderId = 123, Amount = 99.99m };
        var publisher = Substitute.For<IEventPublisher>();

        // Act
        await ExecuteAsync(CreateActivity(eventName, payload: payload), publisher);

        // Assert
        await AssertPublishedAsync(publisher, eventName, payload: payload);
    }

    [Fact]
    public async Task ExecuteAsync_CompletesActivity()
    {
        // Arrange
        var publisher = Substitute.For<IEventPublisher>();

        // Act
        var context = await ExecuteAsync(CreateActivity("TestEvent"), publisher);

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
        var publisher = Substitute.For<IEventPublisher>();

        // Act
        var context = await ExecuteAsync(CreateActivity(eventName, correlationId, isLocalEvent: true, payload: payload), publisher);

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

    private static PublishEvent CreateActivity(
        string eventName,
        string? correlationId = null,
        bool? isLocalEvent = null,
        object? payload = null) =>
        new()
        {
            EventName = new(eventName),
            CorrelationId = correlationId != null ? new(correlationId) : null!,
            IsLocalEvent = isLocalEvent.HasValue ? new(isLocalEvent.Value) : null!,
            Payload = payload != null ? new(payload) : null!
        };

    private static async Task<ActivityExecutionContext> ExecuteAsync(PublishEvent activity, IEventPublisher publisher) =>
        await new ActivityTestFixture(activity)
            .ConfigureServices(services => services.AddSingleton(publisher))
            .ExecuteAsync();

    private static async Task AssertPublishedAsync(
        IEventPublisher publisher,
        string eventName,
        string? correlationId = null,
        string? workflowInstanceId = null,
        object? payload = null,
        bool expectNullCorrelationId = false,
        bool expectNullWorkflowInstanceId = false)
    {
        var correlationIdArg = expectNullCorrelationId
            ? Arg.Is<string?>(x => x == null)
            : correlationId ?? Arg.Any<string?>();

        var workflowInstanceIdArg = expectNullWorkflowInstanceId
            ? Arg.Is<string?>(x => x == null)
            : workflowInstanceId ?? Arg.Any<string?>();

        await publisher.Received(1).PublishAsync(
            eventName,
            correlationIdArg,
            workflowInstanceIdArg,
            Arg.Any<string?>(),
            payload ?? Arg.Any<object?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }
}
