using System.Collections.Generic;
using Elsa.Extensions;

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

public class CustomDictionary : Dictionary<string, string[]>
{
    public string? ContentType => this.GetValue("content-type")?[0];
}