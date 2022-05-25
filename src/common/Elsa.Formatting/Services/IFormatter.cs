namespace Elsa.Formatting.Services;

/// <summary>
/// Represents a formatter that can serialize an object to a string and deserialize a string into an object.
/// </summary>
public interface IFormatter
{
    ValueTask<string> ToStringAsync(object body, CancellationToken cancellationToken = default);
    ValueTask<object> FromStringAsync(string data, Type? returnType, CancellationToken cancellationToken = default);
}