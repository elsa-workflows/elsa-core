using Elsa.JavaScript.Options;
using Elsa.JavaScript.TypeDefinitions.Abstractions;
using Elsa.JavaScript.TypeDefinitions.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.JavaScript.Providers;

[UsedImplicitly]
internal class WorkflowVariablesVariableProvider(IOptions<JintOptions> options) : VariableDefinitionProvider
{
    protected override IEnumerable<VariableDefinition> GetVariableDefinitions(TypeDefinitionContext context)
    {
        if(options.Value.DisableWrappers)
            yield break;
        
        yield return CreateVariableDefinition(x => x.Name("variables").Type("WorkflowVariables"));
    }
}