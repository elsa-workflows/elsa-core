using Elsa.Api.Client.Resources.VariableTypes.Models;

namespace Elsa.Api.Client.Resources.VariableTypes.Responses;

/// <summary>
/// A response containing a list of variable types.
/// </summary>
/// <param name="Items">A list of variable types.</param>
public record ListVariableTypesResponse(ICollection<VariableTypeDescriptor> Items);