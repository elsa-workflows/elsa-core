using Elsa.JavaScript.TypeDefinitions.Models;

namespace Elsa.JavaScript.TypeDefinitions.Builders;

/// <summary>
/// A builder API for building <see cref="FunctionDefinition"/>s.
/// </summary>
public class FunctionDefinitionBuilder
{
    private readonly FunctionDefinition _functionDefinition = new();

    /// <summary>
    /// Set the name of the function.
    /// </summary>
    public FunctionDefinitionBuilder Name(string name)
    {
        _functionDefinition.Name = name;
        return this;
    }

    /// <summary>
    /// Set the return type of the function.
    /// </summary>
    public FunctionDefinitionBuilder ReturnType(string? type)
    {
        _functionDefinition.ReturnType = type;
        return this;
    }

    /// <summary>
    /// Add a parameter to the function.
    /// </summary>
    public FunctionDefinitionBuilder Parameter(string name, string? type, bool isOptional = false)
    {
        _functionDefinition.Parameters.Add(new (name, type, isOptional));
        return this;
    }

    /// <summary>
    /// Build a <see cref="FunctionDefinition"/> using the collected information.
    /// </summary>
    public FunctionDefinition BuildFunctionDefinition() => new(_functionDefinition.Name, _functionDefinition.ReturnType, _functionDefinition.Parameters);
}