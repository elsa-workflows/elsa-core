using Elsa.Common.Models;
using Elsa.JavaScript.Contracts;
using Elsa.JavaScript.TypeDefinitions.Abstractions;
using Elsa.JavaScript.TypeDefinitions.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Humanizer;

namespace Elsa.Workflows.Management.Scripting.JavaScript;

/// <summary>
/// Produces <see cref="FunctionDefinition"/>s for common functions.
/// </summary>
internal class InputFunctionsDefinitionProvider : FunctionDefinitionProvider
{
    private readonly ITypeAliasRegistry _typeAliasRegistry;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;

    public InputFunctionsDefinitionProvider(ITypeAliasRegistry typeAliasRegistry, IWorkflowDefinitionService workflowDefinitionService)
    {
        _typeAliasRegistry = typeAliasRegistry;
        _workflowDefinitionService = workflowDefinitionService;
    }
    
    protected override async ValueTask<IEnumerable<FunctionDefinition>> GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var workflow = context.Workflow;
        var workflowDefinition = await _workflowDefinitionService.FindAsync(workflow.Identity.DefinitionId, VersionOptions.SpecificVersion(workflow.Identity.Version), cancellationToken);
        return workflowDefinition == null ? Array.Empty<FunctionDefinition>() : GetFunctionDefinitionsAsync(workflowDefinition);
    }
    
    private IEnumerable<FunctionDefinition> GetFunctionDefinitionsAsync(WorkflowDefinition workflowDefinition)
    {
        // Input argument getters.
        foreach (var input in workflowDefinition.Inputs)
        {
            var pascalName = input.Name.Pascalize();
            var variableType = input.Type;
            var typeAlias = _typeAliasRegistry.TryGetAlias(variableType, out var alias) ? alias : "any";

            // get{Input}.
            yield return CreateFunctionDefinition(builder => builder.Name($"get{pascalName}").ReturnType(typeAlias));
        }
    }
}