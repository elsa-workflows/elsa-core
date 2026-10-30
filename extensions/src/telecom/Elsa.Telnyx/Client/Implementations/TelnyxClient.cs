using Elsa.Telnyx.Client.Services;

namespace Elsa.Telnyx.Client.Implementations;

/// <summary>
/// Represents a Telnyx API client.
/// </summary>
public class TelnyxClient : ITelnyxClient
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public TelnyxClient(ICallsApi calls, INumberLookupApi numberLookup)
    {
        Calls = calls;
        NumberLookup = numberLookup;
    }
        
    /// <summary>
    /// Provides access to the Calls API.
    /// </summary>
    public ICallsApi Calls { get; }
        
    /// <summary>
    /// Provides access to the Number Lookup API.
    /// </summary>
    public INumberLookupApi NumberLookup { get; }
}