using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoMapper.Internal;
using Elsa.Models;

namespace Elsa.Scripting.JavaScript.Services
{
    public class TypeScriptDefinitionService : ITypeScriptDefinitionService
    {
        private readonly IEnumerable<ITypeDefinitionProvider> _providers;

        public TypeScriptDefinitionService(IEnumerable<ITypeDefinitionProvider> providers)
        {
            _providers = providers;
        }

        public string GenerateTypeScriptDefinition(WorkflowDefinition? workflowDefinition = default)
        {
            var builder = new StringBuilder();
            var types = CollectTypes(workflowDefinition);

            // Render type declarations for anything except those listed in TypeConverters.
            foreach (var type in types)
            {
                var shouldRenderDeclaration = ShouldRenderTypeDeclaration(type);

                if (shouldRenderDeclaration)
                    RenderTypeDeclaration(type, builder);
            }

            if (workflowDefinition != null)
            {
                var contextType = workflowDefinition.ContextOptions?.ContextType;

                if (contextType != null)
                    builder.AppendLine($"declare const workflowContext: {contextType.Name}");
            }

            return builder.ToString();
        }

        private IEnumerable<Type> CollectTypes(WorkflowDefinition? workflowDefinition = default)
        {
            var collectedTypes = new HashSet<Type>();

            if (workflowDefinition != null)
            {
                var contextType = workflowDefinition.ContextOptions?.ContextType;

                if (contextType != null)
                    CollectType(contextType, collectedTypes);
            }

            return collectedTypes;
        }

        private void CollectType(Type type, HashSet<Type> collectedTypes)
        {
            collectedTypes.Add(type);

            // Collect generic type argument types.
            foreach (var typeArgType in type.GenericTypeArguments.Where(x => !collectedTypes.Contains(x)))
            {
                collectedTypes.Add(typeArgType);
                CollectType(typeArgType, collectedTypes);
            }

            // Collect property types.
            var propertyTypes = type.GetProperties().Select(x => x.PropertyType).Where(x => !collectedTypes.Contains(x));

            foreach (var propertyType in propertyTypes)
            {
                collectedTypes.Add(propertyType);
                CollectType(propertyType, collectedTypes);
            }
        }

        private void RenderTypeDeclaration(Type type, StringBuilder output)
        {
            RenderTypeDeclaration("interface", type, output);
        }

        private void RenderTypeDeclaration(string symbol, Type type, StringBuilder output)
        {
            var typeName = type.Name;
            var properties = type.GetProperties();

            output.AppendLine($"declare {symbol} {typeName} {{");

            foreach (var property in properties)
            {
                var typeScriptType = GetTypeScriptType(property.PropertyType);
                var propertyName = property.PropertyType.IsNullableType() ? $"{property.Name}?" : property.Name;
                output.AppendLine($"{propertyName}: {typeScriptType};");
            }

            output.AppendLine("}");
        }

        private string GetTypeScriptType(Type type)
        {
            if (type.IsNullableType())
                type = type.GetTypeOfNullable();

            var provider = _providers.FirstOrDefault(x => x.SupportsType(type));
            return provider == null ? "any" : provider.GetTypeDefinition(type);
        }

        private bool ShouldRenderTypeDeclaration(Type type)
        {
            var provider = _providers.FirstOrDefault(x => x.SupportsType(type));
            return provider == null;
        }
    }
}