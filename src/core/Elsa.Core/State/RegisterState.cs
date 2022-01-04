using Elsa.Models;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// Required for JSON serialization configured with reference handling.

namespace Elsa.State;

public class RegisterState
{
    // ReSharper disable once UnusedMember.Global
    // Required for JSON serialization configured with reference handling.
    public RegisterState()
    {
    }
    
    public RegisterState(IDictionary<string, RegisterLocation> locations)
    {
        Locations = locations;
    }

    public IDictionary<string, RegisterLocation> Locations { get; set; } = default!;
}