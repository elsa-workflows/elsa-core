using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Scripting.JavaScript.Services
{
    public abstract class TypeDefinitionProvider : ITypeDefinitionProvider
    {
        public virtual bool SupportsType(TypeDefinitionContext context, Type type) => false;
        public virtual bool ShouldRenderType(TypeDefinitionContext context, Type type) => false;
        public virtual string GetTypeDefinition(TypeDefinitionContext context, Type type) => "any";
        public virtual ValueTask<IEnumerable<Type>> CollectTypesAsync(TypeDefinitionContext context, CancellationToken cancellationToken = default) => new(CollectTypes(context));
        public virtual IEnumerable<Type> CollectTypes(TypeDefinitionContext context) => Enumerable.Empty<Type>();
    }
}