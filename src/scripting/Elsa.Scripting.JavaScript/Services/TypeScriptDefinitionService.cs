using System;
using System.Collections.Generic;
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

            if (workflowDefinition != null)
            {
                var contextType = workflowDefinition.ContextOptions?.ContextType;

                if (contextType != null)
                {
                    RenderTypeDeclaration(contextType, builder);
                    builder.AppendLine("declare const context: Document");
                }
            }

            return builder.ToString();
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
                { } t when typeof(IEnumerable<>).IsAssignableFrom(t) => $"Array<{t.GenericTypeArguments[0]}>",
                _ => type.Name
            };
        }
    }
}