using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AutoMapper.Internal;
using Elsa.Models;
using NodaTime;

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
                    RenderTypeDeclaration(type, types, builder);
            }

            if (workflowDefinition != null)
            {
                var contextType = workflowDefinition.ContextOptions?.ContextType;

                if (contextType != null)
                {
                    var typeScriptType = GetTypeScriptType(contextType, types);
                    builder.AppendLine($"declare const workflowContext: {typeScriptType}");
                }

                foreach (var variable in workflowDefinition.Variables!.Data)
                {
                    var variableType = variable.Value?.GetType() ?? typeof(object);
                    var typeScriptType = GetTypeScriptType(variableType, types);
                    builder.AppendLine($"declare const {variable.Key}: {typeScriptType}");
                }
            }

            return builder.ToString();
        }

        private HashSet<Type> CollectTypes(WorkflowDefinition? workflowDefinition = default)
        {
            var collectedTypes = new HashSet<Type>();

            if (workflowDefinition != null)
            {
                var contextType = workflowDefinition.ContextOptions?.ContextType;

                if (contextType != null)
                    CollectType(contextType, collectedTypes);

                foreach (var variable in workflowDefinition.Variables!.Data.Values)
                    CollectType(variable!.GetType(), collectedTypes);
            }

            CollectType<Instant>(collectedTypes);
            CollectType<Duration>(collectedTypes);
            CollectType<Period>(collectedTypes);
            CollectType<LocalDate>(collectedTypes);
            CollectType<LocalTime>(collectedTypes);
            CollectType<LocalDateTime>(collectedTypes);

            return collectedTypes;
        }

        private static void CollectType<T>(ISet<Type> collectedTypes) => CollectType(typeof(T), collectedTypes);

        private static void CollectType(Type type, ISet<Type> collectedTypes)
        {
            if (type.IsNullableType())
            {
                CollectType(type.GetTypeOfNullable(), collectedTypes);
                return;
            }

            if (collectedTypes.Contains(type))
                return;

            collectedTypes.Add(type);

            // Collect generic type argument types.
            foreach (var typeArgType in type.GenericTypeArguments.Where(x => !collectedTypes.Contains(x)))
                CollectType(typeArgType, collectedTypes);

            // Collect property types.
            var propertyTypes = type.GetProperties().Select(x => x.PropertyType).Where(x => !collectedTypes.Contains(x));

            foreach (var propertyType in propertyTypes)
                CollectType(propertyType, collectedTypes);

            // Collect method return and argument types.
            var methods = type.GetMethods(BindingFlags.Public);

            foreach (var method in methods)
            {
                var returnType = method.ReturnType;

                if (returnType != typeof(void))
                    CollectType(returnType, collectedTypes);

                var argTypes = method.GetParameters().Select(x => x.ParameterType).ToList();

                foreach (var argType in argTypes)
                    CollectType(argType, collectedTypes);
            }
        }

        private void RenderTypeDeclaration(Type type, HashSet<Type> collectedTypes, StringBuilder output)
        {
            RenderTypeDeclaration("class", type, collectedTypes, output);
        }

        private void RenderTypeDeclaration(string symbol, Type type, HashSet<Type> collectedTypes, StringBuilder output)
        {
            var typeName = type.Name;
            var properties = type.GetProperties();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(x => !x.IsSpecialName).ToList();

            output.AppendLine($"declare {symbol} {typeName} {{");

            foreach (var property in properties)
            {
                var typeScriptType = GetTypeScriptType(property.PropertyType, collectedTypes);
                var propertyName = property.PropertyType.IsNullableType() ? $"{property.Name}?" : property.Name;
                output.AppendLine($"{propertyName}: {typeScriptType};");
            }

            foreach (var method in methods)
            {
                if (method.IsStatic)
                    output.Append("static ");

                output.Append($"{method.Name}(");

                var arguments = method.GetParameters().Select(x => $"{x.Name}:{GetTypeScriptType(x.ParameterType, collectedTypes)}");
                output.Append(string.Join(", ", arguments));
                output.Append(")");

                var returnType = method.ReturnType;
                if (returnType != typeof(void))
                    output.AppendFormat(":{0}", GetTypeScriptType(returnType, collectedTypes));

                output.AppendLine(";");
            }

            output.AppendLine("}");
        }

        private string GetTypeScriptType(Type type, HashSet<Type> collectedTypes)
        {
            if (type.IsNullableType())
                type = type.GetTypeOfNullable();

            var provider = _providers.FirstOrDefault(x => x.SupportsType(type));
            return provider != null ? provider.GetTypeDefinition(type) : collectedTypes.Contains(type) ? type.Name : "any";
        }

        private bool ShouldRenderTypeDeclaration(Type type)
        {
            var provider = _providers.FirstOrDefault(x => x.SupportsType(type));
            return provider == null;
        }
    }
}