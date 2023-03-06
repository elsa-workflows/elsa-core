namespace Elsa.Workflows.Core.Attributes;

/// <summary>
/// Indicates that a property should be excluded from the hash computation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ExcludeFromHashAttribute : Attribute
{
}