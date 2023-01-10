namespace Elsa.Activities.Telnyx.Client.Services
{
    public interface ITelnyxClient
    {
        ICallsApi Calls { get; }
        INumberLookupApi NumberLookup { get; }
    }
}