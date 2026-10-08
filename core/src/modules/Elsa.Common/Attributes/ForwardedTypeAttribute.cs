namespace Elsa.Common;

/// <summary>
/// Helps to forward types to new types when deserializing JSON containing types that have been renamed and or moved to a different namespace.
/// </summary>
/// <param name="type">The new type to forward to.</param>
public class ForwardedTypeAttribute(Type type) : Attribute
{
    public Type NewType { get; set; } = type;
}