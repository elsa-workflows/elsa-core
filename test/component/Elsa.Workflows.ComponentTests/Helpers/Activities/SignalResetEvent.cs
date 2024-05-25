namespace Elsa.Workflows.ComponentTests.Activities;

public class SignalResetEvent : CodeActivity
{
    public SignalResetEvent(string eventName)
    {
        EventName = eventName;
    }

    public string EventName { get; set; }

    protected override void Execute(ActivityExecutionContext context)
    {
        var testEventManager = context.GetRequiredService<ISignalManager>();
        testEventManager.Trigger(EventName);
    }
}