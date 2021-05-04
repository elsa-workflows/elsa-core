namespace Elsa.Activities.Telnyx.Client.Services
{
    public class TelnyxClient : ITelnyxClient
    {
        public TelnyxClient(ICallsApi calls)
        {
            Calls = calls;
        }
        
        public ICallsApi Calls { get; }
    }
}