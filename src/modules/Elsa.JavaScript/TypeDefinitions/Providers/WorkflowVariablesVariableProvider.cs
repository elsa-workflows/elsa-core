using Elsa.JavaScript.TypeDefinitions.Abstractions;
using Elsa.JavaScript.TypeDefinitions.Models;
using JetBrains.Annotations;

namespace Elsa.JavaScript.TypeDefinitions.Providers;

[UsedImplicitly]
internal class WorkflowVariablesVariableProvider : VariableDefinitionProvider
{
    protected override IEnumerable<VariableDefinition> GetVariableDefinitions(TypeDefinitionContext context)
    {
        yield return CreateVariableDefinition(x => x.Name("variables").Type("WorkflowVariables"));
    }
}