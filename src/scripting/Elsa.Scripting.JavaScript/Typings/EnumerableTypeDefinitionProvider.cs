using System;
using System.Collections;
using System.Linq;
using Elsa.Scripting.JavaScript.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.JavaScript.Typings
{
    public class EnumerableTypeDefinitionProvider : TypeDefinitionProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public EnumerableTypeDefinitionProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override bool SupportsType(TypeDefinitionContext context, Type type) => typeof(string) != type && typeof(IEnumerable).IsAssignableFrom(type);

        public override string GetTypeDefinition(TypeDefinitionContext context, Type type)
        {
            var providers = _serviceProvider.GetServices<ITypeDefinitionProvider>().Where(x => x is not EnumerableTypeDefinitionProvider).ToList();
            var elementType = type.IsArray ? type.GetElementType()! : type.GetGenericArguments().FirstOrDefault();
            var typeScriptType = elementType != null ? providers.FirstOrDefault(x => x.SupportsType(context, elementType))?.GetTypeDefinition(context, elementType) : null;

            return typeScriptType == null ? "[]" : $"Array<{typeScriptType}>";
        }
    }
}