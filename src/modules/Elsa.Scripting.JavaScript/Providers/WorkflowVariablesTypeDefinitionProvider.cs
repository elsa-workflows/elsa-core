using Elsa.Extensions;
using Elsa.Scripting.JavaScript.Extensions;
using Elsa.Scripting.JavaScript.Options;
using Elsa.Scripting.JavaScript.TypeDefinitions.Abstractions;
using Elsa.Scripting.JavaScript.TypeDefinitions.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Scripting.JavaScript.Providers;

[UsedImplicitly]
internal class WorkflowVariablesTypeDefinitionProvider(IOptions<JintOptions> options) : TypeDefinitionProvider
{
    protected override IEnumerable<TypeDefinition> GetTypeDefinitions(TypeDefinitionContext context)
    {
        if(options.Value.DisableWrappers)
            yield break;
        
        var variables = context.WorkflowGraph.Workflow.Variables;
        
        var workflowTypeDefinition = new TypeDefinition
        {
            Name = "WorkflowVariables",
            DeclarationKeyword = "class"
        };

        foreach (var variable in variables.Where(x => x.Name.IsValidVariableName()))
        {
            var variableType = variable.GetVariableType();
            workflowTypeDefinition.Properties.Add(new PropertyDefinition
            {
                Name = variable.Name,
                Type = variableType.Name
            });
        }
        
        yield return workflowTypeDefinition;
    }
}