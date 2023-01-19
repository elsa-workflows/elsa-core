using Elsa.JavaScript.Models;

namespace Elsa.JavaScript.Builders;

/// <summary>
/// A builder API for building <see cref="TypeDefinition"/>s.
/// </summary>
public class TypeDefinitionBuilder
{
    private readonly TypeDefinition _typeDefinition = new();


    /// <summary>
    /// Build a <see cref="TypeDefinition"/> using the collected information.
    /// </summary>
    public TypeDefinition BuildTypeDefinition() => new();
}