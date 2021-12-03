using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Models;

namespace Elsa.Scripting.JavaScript.Services
{
    public interface ITypeDefinitionProvider
    {
        bool SupportsType(TypeDefinitionContext context, Type type);
        bool ShouldRenderType(TypeDefinitionContext context, Type type);
        string GetTypeDefinition(TypeDefinitionContext context, Type type);
        ValueTask<IEnumerable<Type>> CollectTypesAsync(TypeDefinitionContext context, CancellationToken cancellationToken = default);
    }

    public record TypeDefinitionContext(WorkflowDefinition? WorkflowDefinition, IntellisenseContext? Context)
    {
        public Func<ITypeDefinitionProvider, Type, string> GetTypeScriptType { get; internal set; } = default!;
    }
}