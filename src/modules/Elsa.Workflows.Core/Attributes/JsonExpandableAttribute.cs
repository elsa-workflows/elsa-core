namespace Elsa.Workflows.Core.Attributes;

/// <summary>
/// Used by a custom converter to indicate that the property should be expanded into a JSON object.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class JsonExpandableAttribute : Attribute
{
}