using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper.Internal;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Events;
using MediatR;
using NodaTime;

namespace Elsa.Scripting.JavaScript.Services
{
    public class TypeScriptDefinitionService : ITypeScriptDefinitionService
    {
        private readonly IEnumerable<ITypeDefinitionProvider> _providers;
        private readonly IMediator _mediator;

        public TypeScriptDefinitionService(IEnumerable<ITypeDefinitionProvider> providers, IMediator mediator)
        {
            _providers = providers;
            _mediator = mediator;
        }

        public async Task<string> GenerateTypeScriptDefinitionsAsync(WorkflowDefinition? workflowDefinition = default, string? context = default, CancellationToken cancellationToken = default)
        {
            var builder = new StringBuilder();
            var types = await CollectTypesAsync(workflowDefinition, cancellationToken);
            
            // Render type declarations for anything except those listed in TypeConverters.
            foreach (var type in types)
            {
                var shouldRenderDeclaration = ShouldRenderTypeDeclaration(type);

                if (shouldRenderDeclaration)
                    RenderTypeDeclaration(type, types, builder);
            }
            
            string GetTypeScriptTypeInternal(Type type) => GetTypeScriptType(type, types);

            if (workflowDefinition != null)
            {
                var contextType = workflowDefinition.ContextOptions?.ContextType;

                if (contextType != null)
                {
                    var typeScriptType = GetTypeScriptTypeInternal(contextType);
                    builder.AppendLine($"declare const workflowContext: {typeScriptType}");
                }

                foreach (var variable in workflowDefinition.Variables!.Data)
                {
                    var variableType = variable.Value?.GetType() ?? typeof(object);
                    var typeScriptType = GetTypeScriptTypeInternal(variableType);
                    builder.AppendLine($"declare const {variable.Key}: {typeScriptType}");
                }
            }

            var renderingTypeScriptDefinitions = new RenderingTypeScriptDefinitions(workflowDefinition, GetTypeScriptTypeInternal, context, builder);
            await _mediator.Publish(renderingTypeScriptDefinitions, cancellationToken);

            return builder.ToString();
        }

        private async Task<ISet<Type>> CollectTypesAsync(WorkflowDefinition? workflowDefinition = default, CancellationToken cancellationToken = default)
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

            void CollectTypesInternal(IEnumerable<Type> types)
            {
                foreach (var type in types) 
                    CollectType(type, collectedTypes);
            }

            var collectTypesEvent = new CollectingTypeScriptDefinitionTypes(workflowDefinition, CollectTypesInternal);
            await _mediator.Publish(collectTypesEvent, cancellationToken);

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

        private void RenderTypeDeclaration(Type type, ISet<Type> collectedTypes, StringBuilder output)
        {
            RenderTypeDeclaration("class", type, collectedTypes, output);
        }

        private void RenderTypeDeclaration(string symbol, Type type, ISet<Type> collectedTypes, StringBuilder output)
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

        private string GetTypeScriptType(Type type, ISet<Type> collectedTypes)
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