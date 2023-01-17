using System.Reflection;
using Elsa.Extensions;
using Elsa.JavaScript.Models;
using Elsa.JavaScript.Services;

namespace Elsa.JavaScript.Implementations;

/// <inheritdoc />
public class TypeDescriber : ITypeDescriber
{
    /// <inheritdoc />
    public TypeDefinition DescribeType(Type type)
    {
        var typeDefinition = new TypeDefinition
        {
            DeclarationKeyword = GetDeclarationKeyword(type),
            Name = type.Name,
            Fields = GetFieldDefinitions(type).ToList(),
            Methods = GetMethodDefinitions(type).ToList()
        };

        return typeDefinition;
    }

    private IEnumerable<FunctionDefinition> GetMethodDefinitions(Type type)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(x => !x.IsSpecialName).ToList();

        foreach (var method in methods)
        {
            yield return new FunctionDefinition
            {
                Name = method.Name,
                Parameters = GetMethodParameters(method).ToList(),
                ReturnType = method.ReturnType.Name
            };
        }
    }

    private static IEnumerable<ParameterDefinition> GetMethodParameters(MethodInfo method)
    {
        var parameters = method.GetParameters();

        foreach (var parameter in parameters)
        {
            yield return new ParameterDefinition
            {
                Name = parameter.Name!,
                Type = parameter.ParameterType,
                IsOptional = parameter.IsOptional
            };
        }
    }

    private static IEnumerable<FieldDefinition> GetFieldDefinitions(Type type)
    {
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            yield return new FieldDefinition
            {
                Name = property.Name,
                Type = property.PropertyType.Name,
                IsOptional = property.PropertyType.IsNullableType()
            };
        }
    }

    private static string GetDeclarationKeyword(Type type) =>
        type switch
        {
            { IsInterface: true } => "interface",
            { IsClass: true } => "class",
            { IsValueType: true } => "class",
            { IsEnum: true } => "enum",
            _ => "interface"
        };
}