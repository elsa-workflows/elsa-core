using Elsa.Common.Models;
using Elsa.JavaScript.Contracts;
using Elsa.JavaScript.Helpers;
using Elsa.JavaScript.Options;
using Elsa.JavaScript.TypeDefinitions.Abstractions;
using Elsa.JavaScript.TypeDefinitions.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.JavaScript.Providers;

/// Produces <see cref="FunctionDefinition"/>s for common functions.
[UsedImplicitly]
internal class InputFunctionsDefinitionProvider(ITypeAliasRegistry typeAliasRegistry, IWorkflowDefinitionService workflowDefinitionService, IOptions<JintOptions> options) : FunctionDefinitionProvider
{
    protected override async ValueTask<IEnumerable<FunctionDefinition>> GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        if (options.Value.DisableWrappers)
            return [];
        
        var cancellationToken = context.CancellationToken;
        var workflow = context.Workflow;
        var workflowDefinition = await workflowDefinitionService.FindWorkflowDefinitionAsync(workflow.Identity.DefinitionId, VersionOptions.SpecificVersion(workflow.Identity.Version), cancellationToken);
        return workflowDefinition == null ? Array.Empty<FunctionDefinition>() : GetFunctionDefinitionsAsync(workflowDefinition);
    }
    
    private IEnumerable<FunctionDefinition> GetFunctionDefinitionsAsync(WorkflowDefinition workflowDefinition)
    {
        // Input argument getters.
        foreach (var input in workflowDefinition.Inputs.Where(x => VariableNameValidator.IsValidVariableName(x.Name)))
        {
            var pascalName = input.Name.Pascalize();
            var variableType = input.Type;
            var typeAlias = typeAliasRegistry.TryGetAlias(variableType, out var alias) ? alias : "any";

            // get{Input}.
            yield return CreateFunctionDefinition(builder => builder.Name($"get{pascalName}").ReturnType(typeAlias));
        }
    }
}