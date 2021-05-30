using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Scripting.JavaScript.Services
{
    public interface ITypeDefinitionProvider
    {
        bool SupportsType(TypeDefinitionContext context, Type type);
        string GetTypeDefinition(TypeDefinitionContext context, Type type);
        ValueTask<IEnumerable<Type>> CollectTypesAsync(TypeDefinitionContext context, CancellationToken cancellationToken = default);
    }

    public record TypeDefinitionContext(WorkflowDefinition? WorkflowDefinition, string? context);
}