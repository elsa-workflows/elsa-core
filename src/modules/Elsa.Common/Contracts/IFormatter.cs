namespace Elsa.Common.Contracts;

/// <summary>
/// Represents a formatter that can serialize an object to and from a string.
/// </summary>
public interface IFormatter
{
    /// <summary>
    /// Serializes the specified value into a string. 
    /// </summary>
    ValueTask<string> ToStringAsync(object value, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deserializes the specified string into the specified return type.
    /// </summary>
    ValueTask<object> FromStringAsync(string data, Type? returnType, CancellationToken cancellationToken = default);
}