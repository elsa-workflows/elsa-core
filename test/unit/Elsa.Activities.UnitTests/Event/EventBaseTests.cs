using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Activities.UnitTests.Event;

public class EventBaseTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesBookmark_WithCorrectEventName()
    {
        // Arrange
        const string eventName = "MyTestEvent";
        var activity = new TestEvent(eventName);

        // Act
        var context = await ExecuteAsync(activity);

        // Assert
        Assert.Equal(ActivityStatus.Running, context.Status);
        var stimulus = GetEventStimulusFromContext(context);
        Assert.Equal(eventName, stimulus.EventName);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesBookmark_WithoutActivityInstanceId()
    {
        // Arrange
        var activity = new TestEvent("TestEvent");

        // Act
        var context = await ExecuteAsync(activity);

        // Assert
        var bookmark = Assert.Single(context.WorkflowExecutionContext.Bookmarks);
        Assert.True(string.IsNullOrEmpty(bookmark.ActivityInstanceId),
            "ActivityInstanceId should be null or empty because IncludeActivityInstanceId is set to false");
    }

    [Theory]
    [InlineData("Event.Order.Created")]
    [InlineData("Event.User.Registered")]
    [InlineData("CustomEvent")]
    public async Task ExecuteAsync_CreatesBookmark_WithCorrectEventStimulus(string eventName)
    {
        // Arrange & Act
        var context = await ExecuteAsync(new TestEvent(eventName));

        // Assert
        var stimulus = GetEventStimulusFromContext(context);
        Assert.Equal(eventName, stimulus.EventName);
    }

    [Fact]
    public async Task ExecuteAsync_SetsResultOutput_WhenCallbackIsInvoked()
    {
        // Arrange
        const string expectedInput = "test payload";
        var activity = new TestEvent<string>("TestEvent");

        // Act
        var context = await ExecuteAsync(activity);
        context.WorkflowExecutionContext.Input[Elsa.Workflows.Runtime.Activities.Event.EventInputWorkflowInputKey] = expectedInput;
        await activity.InvokeCallbackAsync(context);

        // Assert
        var result = context.GetActivityOutput(() => activity.Result);
        Assert.Equal(expectedInput, result);
    }

    [Fact]
    public async Task GetTriggerPayload_ReturnsEventStimulus_WithCorrectEventName()
    {
        // Arrange
        const string eventName = "TriggerEvent";
        var activity = new TestEvent(eventName);
        var triggerIndexingContext = await CreateTriggerIndexingContextAsync(activity);

        // Act
        var payload = activity.GetTriggerPayloadPublic(triggerIndexingContext);

        // Assert
        var stimulus = Assert.IsType<EventStimulus>(payload);
        Assert.Equal(eventName, stimulus.EventName);
    }

    [Fact]
    public async Task OnEventReceivedAsync_IsCalledDuringCallback()
    {
        // Arrange
        const string testPayload = "callback-invoked";
        var activity = new TestEvent<string>("TestEvent");

        // Act
        var context = await ExecuteAsync(activity);
        context.WorkflowExecutionContext.Input[Elsa.Workflows.Runtime.Activities.Event.EventInputWorkflowInputKey] = testPayload;
        await activity.InvokeCallbackAsync(context);

        // Assert
        Assert.True(activity.OnEventReceivedAsyncWasCalled);
        Assert.Equal(testPayload, activity.ReceivedInput);
    }

    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity) =>
        new ActivityTestFixture(activity).ExecuteAsync();

    private static EventStimulus GetEventStimulusFromContext(ActivityExecutionContext context)
    {
        var bookmark = Assert.Single(context.WorkflowExecutionContext.Bookmarks);
        Assert.Equal(RuntimeStimulusNames.Event, bookmark.Name);
        return Assert.IsType<EventStimulus>(bookmark.Payload);
    }

    private static async Task<TriggerIndexingContext> CreateTriggerIndexingContextAsync(IActivity activity)
    {
        var fixture = new ActivityTestFixture(activity);
        var activityContext = await fixture.BuildAsync();
        var workflowExecutionContext = activityContext.WorkflowExecutionContext;
        var expressionExecutionContext = new ExpressionExecutionContext(workflowExecutionContext.ServiceProvider, workflowExecutionContext.MemoryRegister);
        var workflowIndexingContext = new WorkflowIndexingContext(workflowExecutionContext.Workflow, CancellationToken.None);
        return new(
            workflowIndexingContext,
            expressionExecutionContext,
            (ITrigger)activity,
            CancellationToken.None);
    }

    // Test implementation of EventBase for unit testing
    private class TestEvent(string eventName) : EventBase<object?>
    {
        protected override string GetEventName(ExpressionExecutionContext context) => eventName;

        public object GetTriggerPayloadPublic(TriggerIndexingContext context) => GetTriggerPayload(context);
    }

    // Test implementation with generic type and callback tracking
    private class TestEvent<TResult>(string eventName) : EventBase<TResult>
    {
        public bool OnEventReceivedAsyncWasCalled { get; private set; }
        public TResult? ReceivedInput { get; private set; }

        protected override string GetEventName(ExpressionExecutionContext context) => eventName;

        protected override ValueTask OnEventReceivedAsync(ActivityExecutionContext context, TResult? input)
        {
            OnEventReceivedAsyncWasCalled = true;
            ReceivedInput = input;
            return base.OnEventReceivedAsync(context, input);
        }

        public ValueTask InvokeCallbackAsync(ActivityExecutionContext context) => EventReceivedAsync(context);
    }
}