using System;
using Elsa.Scripting.JavaScript.Services;

namespace Elsa.Scripting.JavaScript.Typings
{
    public class EnumTypeDefinitionProvider : TypeDefinitionProvider
    {
        public override bool ShouldRenderType(TypeDefinitionContext context, Type type) => type.IsEnum;

        public override bool SupportsType(TypeDefinitionContext context, Type type) => type.IsEnum;

        public override string GetTypeDefinition(TypeDefinitionContext context, Type type) => type.Name;
    }
}