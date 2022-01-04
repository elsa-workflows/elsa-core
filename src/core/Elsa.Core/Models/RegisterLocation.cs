namespace Elsa.Models;

/// <summary>
/// Represents a location within a register
/// </summary>
public class RegisterLocation
{
    public RegisterLocation()
    {
    }

    public RegisterLocation(object? value)
    {
        Value = value;
    }
        
    public object? Value { get; set; }
}