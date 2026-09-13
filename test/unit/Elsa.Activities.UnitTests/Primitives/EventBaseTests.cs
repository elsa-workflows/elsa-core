using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Stimuli;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Primitives;

public class EventBaseTests
{
    [Test]
    public async Task ExecuteAsync_CreatesBookmark_WithCorrectEventName()
    {
        // Arrange
        const string eventName = "MyTestEvent";
        var activity = new TestEvent(eventName);

        // Act
        var context = await ExecuteAsync(activity);

        // Assert
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Running);
        var stimulus = await GetEventStimulusFromContext(context);
        await Assert.That(stimulus.EventName).IsEqualTo(eventName);
    }

    [Test]
    public async Task ExecuteAsync_CreatesBookmark_WithoutActivityInstanceId()
    {
        // Arrange
        var activity = new TestEvent("TestEvent");

        // Act
        var context = await ExecuteAsync(activity);

        // Assert
        var bookmark = await Assert.That(context.WorkflowExecutionContext.Bookmarks).HasSingleItem();
        await Assert.That(string.IsNullOrEmpty(bookmark.ActivityInstanceId)).IsTrue().Because("ActivityInstanceId should be null or empty because IncludeActivityInstanceId is set to false");
    }

    [Test]
    [Arguments("Event.Order.Created")]
    [Arguments("Event.User.Registered")]
    [Arguments("CustomEvent")]
    public async Task ExecuteAsync_CreatesBookmark_WithCorrectEventStimulus(string eventName)
    {
        // Arrange & Act
        var context = await ExecuteAsync(new TestEvent(eventName));

        // Assert
        var stimulus = await GetEventStimulusFromContext(context);
        await Assert.That(stimulus.EventName).IsEqualTo(eventName);
    }

    [Test]
    public async Task ExecuteAsync_SetsResultOutput_WhenCallbackIsInvoked()
    {
        // Arrange
        const string expectedInput = "test payload";
        var activity = new TestEvent<string>("TestEvent");

        // Act
        var context = await ExecuteAsync(activity);
        context.WorkflowExecutionContext.Input[Event.EventInputWorkflowInputKey] = expectedInput;
        await activity.InvokeCallbackAsync(context);

        // Assert
        var result = context.GetActivityOutput(() => activity.Result);
        await Assert.That(result).IsEqualTo(expectedInput);
    }

    [Test]
    public async Task GetTriggerPayload_ReturnsEventStimulus_WithCorrectEventName()
    {
        // Arrange
        const string eventName = "TriggerEvent";
        var activity = new TestEvent(eventName);
        var triggerIndexingContext = await CreateTriggerIndexingContextAsync(activity);

        // Act
        var payload = activity.GetTriggerPayloadPublic(triggerIndexingContext);

        // Assert
        await Assert.That(payload).IsOfType(typeof(EventStimulus));
        var stimulus = (EventStimulus)payload!;
        await Assert.That(stimulus.EventName).IsEqualTo(eventName);
    }

    [Test]
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
        await Assert.That(activity.OnEventReceivedAsyncWasCalled).IsTrue();
        await Assert.That(activity.ReceivedInput).IsEqualTo(testPayload);
    }

    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity) =>
        new ActivityTestFixture(activity).ExecuteAsync();

    private static async Task<EventStimulus> GetEventStimulusFromContext(ActivityExecutionContext context)
    {
        var bookmark = await Assert.That(context.WorkflowExecutionContext.Bookmarks).HasSingleItem();
        await Assert.That(bookmark.Name).IsEqualTo(RuntimeStimulusNames.Event);
        await Assert.That(bookmark.Payload).IsOfType(typeof(EventStimulus));
        return (EventStimulus)bookmark.Payload!;
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
