namespace Elsa.ServiceBusIntegrationTests.Contracts
{
    public interface ITestResetEventManager
    {
        public void Set(string resetEvent);
        public AutoResetEvent Get(string resetEvent);
        public AutoResetEvent Init(string resetEvent);
    }
}