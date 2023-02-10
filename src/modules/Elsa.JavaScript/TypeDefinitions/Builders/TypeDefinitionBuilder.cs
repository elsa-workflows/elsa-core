using Elsa.JavaScript.TypeDefinitions.Models;

namespace Elsa.JavaScript.TypeDefinitions.Builders;

/// <summary>
/// A builder API for building <see cref="TypeDefinition"/>s.
/// </summary>
public class TypeDefinitionBuilder
{
    private readonly TypeDefinition _typeDefinition = new();
    
    /// <summary>
    /// Set the name of the function.
    /// </summary>
    public TypeDefinitionBuilder Name(string name)
    {
        _typeDefinition.Name = name;
        return this;
    }

    /// <summary>
    /// Set the return type of the function.
    /// </summary>
    public TypeDefinitionBuilder DeclarationKeyword(string keyword)
    {
        _typeDefinition.DeclarationKeyword = keyword;
        return this;
    }

    /// <summary>
    /// Build a <see cref="TypeDefinition"/> using the collected information.
    /// </summary>
    public TypeDefinition BuildTypeDefinition() => new();
}