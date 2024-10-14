using Elsa.Extensions;
using Elsa.JavaScript.TypeDefinitions.Abstractions;
using Elsa.JavaScript.TypeDefinitions.Models;
using JetBrains.Annotations;

namespace Elsa.JavaScript.TypeDefinitions.Providers;

[UsedImplicitly]
internal class WorkflowVariablesTypeDefinitionProvider : TypeDefinitionProvider
{
    protected override IEnumerable<TypeDefinition> GetTypeDefinitions(TypeDefinitionContext context)
    {
        var variables = context.WorkflowGraph.Workflow.Variables;
        
        var workflowTypeDefinition = new TypeDefinition
        {
            Name = "WorkflowVariables",
            DeclarationKeyword = "class"
        };

        foreach (var variable in variables)
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