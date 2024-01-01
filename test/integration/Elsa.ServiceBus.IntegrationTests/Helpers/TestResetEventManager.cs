using Elsa.ServiceBus.IntegrationTests.Contracts;

namespace Elsa.ServiceBus.IntegrationTests.Helpers;

public class TestResetEventManager : ITestResetEventManager
{
    public AutoResetEvent WaitHandleTest { get; } = new(false);
    private readonly Dictionary<string, AutoResetEvent> _events = new();

    public AutoResetEvent Get()
    {
        return WaitHandleTest;
    }

    public AutoResetEvent? Get(string resetEvent)
    {
        _events.TryGetValue(resetEvent, out var result);
        return result;
    }

    public AutoResetEvent Init(string resetEvent)
    {
        var autoResetEvent = Get(resetEvent);
        if (autoResetEvent != null) 
            return autoResetEvent;
            
        var newEvent = new AutoResetEvent(false);
        _events.Add(resetEvent, newEvent);
        return newEvent;
    }

    public void Set(string resetEvent)
    {
        Get(resetEvent)?.Set();
    }
}