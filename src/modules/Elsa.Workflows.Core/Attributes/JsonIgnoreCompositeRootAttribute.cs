namespace Elsa.Workflows.Core.Attributes;

/// <summary>
/// Used by <see cref="JsonIgnoreCompositeRootConverter{T}"/> to indicate that the property should be expanded into a JSON object.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class JsonIgnoreCompositeRootAttribute : Attribute
{
}