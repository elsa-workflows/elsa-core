namespace Elsa.Activities.Telnyx.Client.Services
{
    public class TelnyxClient : ITelnyxClient
    {
        public TelnyxClient(ICallsApi calls, INumberLookupApi numberLookup)
        {
            Calls = calls;
            NumberLookup = numberLookup;
        }
        
        public ICallsApi Calls { get; }
        public INumberLookupApi NumberLookup { get; }
    }
}