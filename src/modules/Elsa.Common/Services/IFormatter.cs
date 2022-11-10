namespace Elsa.Common.Services;

/// <summary>
/// Represents a formatter that can serialize an object to and from a string.
/// </summary>
public interface IFormatter
{
    ValueTask<string> ToStringAsync(object body, CancellationToken cancellationToken = default);
    ValueTask<object> FromStringAsync(string data, Type? returnType, CancellationToken cancellationToken = default);
}