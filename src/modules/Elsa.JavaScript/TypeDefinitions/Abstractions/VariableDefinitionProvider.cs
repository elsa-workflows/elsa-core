using Elsa.JavaScript.TypeDefinitions.Builders;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Models;

namespace Elsa.JavaScript.TypeDefinitions.Abstractions;

public abstract class VariableDefinitionProvider : IVariableDefinitionProvider
{
    protected virtual ValueTask<IEnumerable<VariableDefinition>> GetVariableDefinitionsAsync(TypeDefinitionContext context)
    {
        var variables = GetVariableDefinitions(context);
        return new(variables);
    }

    protected virtual IEnumerable<VariableDefinition> GetVariableDefinitions(TypeDefinitionContext context)
    {
        yield break;
    }

    async ValueTask<IEnumerable<VariableDefinition>> IVariableDefinitionProvider.GetVariableDefinitionsAsync(TypeDefinitionContext context) => await GetVariableDefinitionsAsync(context);

    protected VariableDefinition CreateVariableDefinition(Action<VariableDefinitionBuilder> setup)
    {
        var builder = new VariableDefinitionBuilder();
        setup(builder);
        return builder.BuildVariableDefinition();
    }
}