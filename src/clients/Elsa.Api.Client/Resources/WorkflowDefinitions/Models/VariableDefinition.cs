namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// Stores information about a workflow variable.
/// </summary>
public class VariableDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string TypeName { get; set; }
    public bool IsArray { get; set; }
    public string? Value { get; set; }
    public string? StorageDriverTypeName { get; set; }
}