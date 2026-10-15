namespace Elsa.Api.Client.Resources.VariableTypes.Models;

/// <summary>
/// Represents a variable type.
/// </summary>
/// <param name="TypeName">The .NET type name of the variable type.</param>
/// <param name="DisplayName">The display name of the variable type.</param>
/// <param name="Category">The category of the variable type.</param>
/// <param name="Description">The description of the variable type.</param>
public record VariableTypeDescriptor(string TypeName, string DisplayName, string Category, string? Description);