using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using AutoMapper.Internal;
using Elsa.Models;

namespace Elsa.Scripting.JavaScript.Services
{
    public class TypeScriptDefinitionService : ITypeScriptDefinitionService
    {
        
        public string GenerateTypeScriptDefinition(WorkflowDefinition? workflowDefinition = default)
        {
            var builder = new StringBuilder();
            var types = CollectTypes(workflowDefinition);

            foreach (var type in types) 
                RenderTypeDeclaration(type, builder);
            
            if (workflowDefinition != null)
            {
                var contextType = workflowDefinition.ContextOptions?.ContextType;

                if (contextType != null) 
                    builder.AppendLine("declare const context: Document");
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
            var typeName = type.Name;
            var properties = type.GetProperties();

            output.AppendLine($"declare class {typeName} {{");

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
            
            return type switch
            {
                { } t when t == typeof(short) => "number",
                { } t when t == typeof(ushort) => "number",
                { } t when t == typeof(int) => "number",
                { } t when t == typeof(uint) => "number",
                { } t when t == typeof(long) => "number",
                { } t when t == typeof(ulong) => "number",
                { } t when t == typeof(float) => "number",
                { } t when t == typeof(decimal) => "number",
                { } t when t == typeof(string) => "string",
                { } t when t == typeof(DateTime) => "Date",
                { } t when t == typeof(DateTimeOffset) => "Date",
                { } t when typeof(IDictionary<,>).IsAssignableFrom(t) => "any",
                { } t when typeof(IEnumerable).IsAssignableFrom(t) => $"Array<{t.GenericTypeArguments[0].Name}>",
                _ => type.Name
            };
        }
    }
}