using System;
using System.Collections;
using Elsa.Scripting.JavaScript.Services;

namespace Elsa.Scripting.JavaScript.Typings
{
    public class EnumerableTypeDefinitionProvider : TypeDefinitionProvider
    {
        public override bool SupportsType(TypeDefinitionContext context, Type type) => typeof(IEnumerable).IsAssignableFrom(type);
        public override string GetTypeDefinition(TypeDefinitionContext context, Type type) => "[]";
    }
}