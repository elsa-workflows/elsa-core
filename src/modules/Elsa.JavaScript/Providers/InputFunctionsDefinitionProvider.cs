using Elsa.JavaScript.Contracts;
using Elsa.JavaScript.TypeDefinitions.Abstractions;
using Elsa.JavaScript.TypeDefinitions.Models;
using Elsa.Workflows.Activities;
using Humanizer;
using JetBrains.Annotations;

namespace Elsa.JavaScript.Providers;

/// Produces <see cref="FunctionDefinition"/>s for common functions.
[UsedImplicitly]
internal class InputFunctionsDefinitionProvider(ITypeAliasRegistry typeAliasRegistry) : FunctionDefinitionProvider
{
    protected override ValueTask<IEnumerable<FunctionDefinition>> GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        var workflow = context.WorkflowGraph.Workflow;
        return ValueTask.FromResult(GetFunctionDefinitionsAsync(workflow));
    }
    
    private IEnumerable<FunctionDefinition> GetFunctionDefinitionsAsync(Workflow workflow)
    {
        // Input argument getters.
        foreach (var input in workflow.Inputs)
        {
            var pascalName = input.Name.Pascalize();
            var variableType = input.Type;
            var typeAlias = typeAliasRegistry.TryGetAlias(variableType, out var alias) ? alias : "any";

            // get{Input}.
            yield return CreateFunctionDefinition(builder => builder.Name($"get{pascalName}").ReturnType(typeAlias));
        }
    }
}