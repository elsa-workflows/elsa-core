namespace Elsa.Workflows.Management.Attributes;

/// <summary>
/// Indicates that a host method parameter should be resolved from the service provider instead of from workflow inputs.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class FromServicesAttribute : Attribute
{
}

