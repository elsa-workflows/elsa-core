using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Models;

namespace Elsa.JavaScript.TypeDefinitions.Services;

/// <inheritdoc />
public class TypeDefinitionService : ITypeDefinitionService
{
    private readonly IEnumerable<ITypeDefinitionProvider> _typeDefinitionProviders;
    private readonly IEnumerable<IFunctionDefinitionProvider> _functionDefinitionProviders;
    private readonly ITypeDefinitionDocumentRenderer _typeDefinitionDocumentRenderer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public TypeDefinitionService(
        IEnumerable<ITypeDefinitionProvider> typeDefinitionProviders, 
        IEnumerable<IFunctionDefinitionProvider> functionDefinitionProviders, 
        ITypeDefinitionDocumentRenderer typeDefinitionDocumentRenderer)
    {
        _typeDefinitionProviders = typeDefinitionProviders;
        _functionDefinitionProviders = functionDefinitionProviders;
        _typeDefinitionDocumentRenderer = typeDefinitionDocumentRenderer;
    }

    /// <inheritdoc />
    public async Task<string> GenerateTypeDefinitionsAsync(TypeDefinitionContext context)
    {
        var typeDefinitions = await GetTypeDefinitionsAsync(context).ToListAsync(context.CancellationToken);
        var functionDefinitions = await GetFunctionDefinitionsAsync(context).ToListAsync(context.CancellationToken);
        
        var document = new TypeDefinitionsDocument
        {
            Functions = functionDefinitions,
            Types = typeDefinitions
        };
        
        return _typeDefinitionDocumentRenderer.Render(document);
    }

    private async IAsyncEnumerable<FunctionDefinition> GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        foreach (var provider in _functionDefinitionProviders)
        {
            var definitions = await provider.GetFunctionDefinitionsAsync(context);

            foreach (var definition in definitions)
                yield return definition;
        }
    }
    
    private async IAsyncEnumerable<TypeDefinition> GetTypeDefinitionsAsync(TypeDefinitionContext context)
    {
        foreach (var provider in _typeDefinitionProviders)
        {
            var definitions = await provider.GetTypeDefinitionsAsync(context);

            foreach (var definition in definitions)
                yield return definition;
        }
    }
}