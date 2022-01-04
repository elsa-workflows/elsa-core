using Elsa.Models;

namespace Elsa.State;

public class RegisterState
{
    public RegisterState(IDictionary<string, RegisterLocation> locations)
    {
        Locations = locations;
    }

    public IDictionary<string, RegisterLocation> Locations { get; set; }
}