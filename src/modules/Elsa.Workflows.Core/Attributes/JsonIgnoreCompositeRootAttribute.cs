using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.Workflows.Core.Attributes;

/// <summary>
/// Used by <see cref="JsonIgnoreCompositeRootConverter"/> to indicate that the property should be expanded into a JSON object.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class JsonIgnoreCompositeRootAttribute : Attribute
{
}