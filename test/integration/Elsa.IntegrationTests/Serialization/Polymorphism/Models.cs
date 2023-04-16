using System.Collections.Generic;

namespace Elsa.IntegrationTests.Serialization.Polymorphism;

/// <summary>
/// A model that contains a mixed bag of primitive, complex and expando objects.
/// </summary>
public record Model(
    string? Text = default,
    long Number = default,
    bool Flag = default,
    ICollection<Model>? Items = default,
    object? Metadata = default,
    object? Payload = default,
    IDictionary<string, Model>? Properties = default
)
{
    public Model() : this(null, default, default, null, default, default, default)
    {
    }
}