using Elsa.Expressions.Models;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// Required for JSON serialization configured with reference handling.

namespace Elsa.Workflows.Core.State;

public class RegisterState
{
    // ReSharper disable once UnusedMember.Global
    // Required for JSON serialization configured with reference handling.
    public RegisterState()
    {
    }
    
    public RegisterState(IDictionary<string, MemoryBlock> locations)
    {
        Locations = locations;
    }

    public IDictionary<string, MemoryBlock> Locations { get; set; } = default!;
}