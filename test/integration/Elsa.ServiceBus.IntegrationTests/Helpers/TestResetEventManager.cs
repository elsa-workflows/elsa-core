using Elsa.ServiceBus.IntegrationTests.Contracts;

namespace Elsa.ServiceBus.IntegrationTests.Helpers
{
    public class TestResetEventManager : ITestResetEventManager
    {
        public AutoResetEvent WaitHandleTest { get; } = new AutoResetEvent(false);
        private IDictionary<string, AutoResetEvent> _events = new Dictionary<string, AutoResetEvent>();
        public AutoResetEvent Get()
        {
            return WaitHandleTest;
        }

        public AutoResetEvent Get(string resetEvent)
        {
            _events.TryGetValue(resetEvent, out var result);
            return result;
        }

        public AutoResetEvent Init(string resetEvent)
        {
            var rEvent = Get(resetEvent);
            if (rEvent == null)
            {
                var newEvent = new AutoResetEvent(false);
                _events.Add(resetEvent, newEvent);
                return newEvent;
            }
            return rEvent;
        }

        public void Set(string resetEvent)
        {
            Get(resetEvent)?.Set();
        }
    }
}