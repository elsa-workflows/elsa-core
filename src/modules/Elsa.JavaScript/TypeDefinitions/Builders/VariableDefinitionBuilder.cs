using Elsa.JavaScript.TypeDefinitions.Models;

namespace Elsa.JavaScript.TypeDefinitions.Builders;

public class VariableDefinitionBuilder
{
    private readonly VariableDefinition _variableDefinition = new();

    public VariableDefinitionBuilder Name(string name)
    {
        _variableDefinition.Name = name;
        return this;
    }

    public VariableDefinitionBuilder Type(string type)
    {
        _variableDefinition.Type = type;
        return this;
    }

    public VariableDefinition BuildVariableDefinition() => new(_variableDefinition.Name, _variableDefinition.Type);
}