using Elsa.JavaScript.Models;
using Elsa.JavaScript.Services;

namespace Elsa.JavaScript.Implementations;

/// <inheritdoc />
public class TypeDefinitionService : ITypeDefinitionService
{
    /// <inheritdoc />
    public Task<string> GenerateTypeDefinitionsAsync(IntellisenseContext? context = default, CancellationToken cancellationToken = default)
    {
        var typeDefinition = "export interface Sample { foo: string; bar: string; }";
        return Task.FromResult(typeDefinition);
    }
}