namespace Elsa.Models;

/// <summary>
/// Represents a register of memory. 
/// </summary>
public class Register
{
    public Register(IDictionary<string, RegisterLocation>? locations = default)
    {
        Locations = locations ?? new Dictionary<string, RegisterLocation>();
    }
        
    public IDictionary<string, RegisterLocation> Locations { get; }

    public bool TryGetLocation(string id, out RegisterLocation location)
    {
        location = null!;
        return Locations.TryGetValue(id, out location!);
    }

    public void Declare(IEnumerable<RegisterLocationReference> references)
    {
        foreach (var reference in references)
            Declare(reference);
    }

    public RegisterLocation Declare(RegisterLocationReference reference)
    {
        var location = reference.Declare();
        Locations[reference.Id] = location;
        return location;
    }
}