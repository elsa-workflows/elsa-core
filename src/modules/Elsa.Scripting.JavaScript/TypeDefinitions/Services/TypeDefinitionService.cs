using Elsa.Scripting.JavaScript.TypeDefinitions.Contracts;
using Elsa.Scripting.JavaScript.TypeDefinitions.Models;

namespace Elsa.Scripting.JavaScript.TypeDefinitions.Services;

/// <inheritdoc />
public class TypeDefinitionService(
    IEnumerable<ITypeDefinitionProvider> typeDefinitionProviders,
    IEnumerable<IFunctionDefinitionProvider> functionDefinitionProviders,
    IEnumerable<IVariableDefinitionProvider> variableDefinitionProviders,
    ITypeDefinitionDocumentRenderer typeDefinitionDocumentRenderer)
    : ITypeDefinitionService
{
    /// <inheritdoc />
    public async Task<string> GenerateTypeDefinitionsAsync(TypeDefinitionContext context)
    {
        var typeDefinitions = await GetTypeDefinitionsAsync(context).ToListAsync(context.CancellationToken);
        var functionDefinitions = await GetFunctionDefinitionsAsync(context).ToListAsync(context.CancellationToken);
        var variableDefinitions = await GetVariableDefinitionsAsync(context).ToListAsync(context.CancellationToken);

        var document = new TypeDefinitionsDocument
        {
            Functions = functionDefinitions,
            Types = typeDefinitions,
            Variables = variableDefinitions
        };

        return typeDefinitionDocumentRenderer.Render(document);
    }

    private async IAsyncEnumerable<FunctionDefinition> GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        foreach (var provider in functionDefinitionProviders)
        {
            var definitions = await provider.GetFunctionDefinitionsAsync(context);

            foreach (var definition in definitions)
                yield return definition;
        }
    }

    private async IAsyncEnumerable<TypeDefinition> GetTypeDefinitionsAsync(TypeDefinitionContext context)
    {
        foreach (var provider in typeDefinitionProviders)
        {
            var definitions = await provider.GetTypeDefinitionsAsync(context);

            foreach (var definition in definitions)
                yield return definition;
        }
    }
    
    private async IAsyncEnumerable<VariableDefinition> GetVariableDefinitionsAsync(TypeDefinitionContext context)
    {
        foreach (var provider in variableDefinitionProviders)
        {
            var definitions = await provider.GetVariableDefinitionsAsync(context);

            foreach (var definition in definitions)
                yield return definition;
        }
    }
}