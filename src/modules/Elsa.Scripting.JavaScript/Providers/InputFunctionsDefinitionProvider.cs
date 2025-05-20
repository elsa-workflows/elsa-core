using Elsa.Scripting.JavaScript.Contracts;
using Elsa.Scripting.JavaScript.Helpers;
using Elsa.Scripting.JavaScript.Options;
using Elsa.Scripting.JavaScript.TypeDefinitions.Abstractions;
using Elsa.Scripting.JavaScript.TypeDefinitions.Models;
using Elsa.Workflows.Activities;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Scripting.JavaScript.Providers;

/// <summary>
/// Produces <see cref="FunctionDefinition"/>s for common functions.
/// </summary>
[UsedImplicitly]
internal class InputFunctionsDefinitionProvider(ITypeAliasRegistry typeAliasRegistry, IOptions<JintOptions> options) : FunctionDefinitionProvider
{
    protected override ValueTask<IEnumerable<FunctionDefinition>> GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        if(options.Value.DisableWrappers)
            return ValueTask.FromResult<IEnumerable<FunctionDefinition>>([]);
        
        var workflow = context.WorkflowGraph.Workflow;
        return ValueTask.FromResult(GetFunctionDefinitionsAsync(workflow));
    }
    
    private IEnumerable<FunctionDefinition> GetFunctionDefinitionsAsync(Workflow workflow)
    {
        // Input argument getters.
        foreach (var input in workflow.Inputs.Where(x => VariableNameValidator.IsValidVariableName(x.Name)))
        {
            var pascalName = input.Name.Pascalize();
            var variableType = input.Type;
            var typeAlias = typeAliasRegistry.TryGetAlias(variableType, out var alias) ? alias : "any";

            // get{Input}.
            yield return CreateFunctionDefinition(builder => builder.Name($"get{pascalName}").ReturnType(typeAlias));
        }
    }
}