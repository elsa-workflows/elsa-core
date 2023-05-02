using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Extensions;
using Elsa.JavaScript.Contracts;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Models;

namespace Elsa.JavaScript.TypeDefinitions.Services;

/// <inheritdoc />
public class TypeDescriber : ITypeDescriber
{
    private readonly ITypeAliasRegistry _typeAliasRegistry;

    /// <summary>
    /// Constructor.
    /// </summary>
    public TypeDescriber(ITypeAliasRegistry typeAliasRegistry)
    {
        _typeAliasRegistry = typeAliasRegistry;
    }
    
    /// <inheritdoc />
    public TypeDefinition DescribeType(Type type)
    {
        var typeDefinition = new TypeDefinition
        {
            DeclarationKeyword = GetDeclarationKeyword(type),
            Name = type.Name,
            Properties = GetPropertyDefinitions(type).DistinctBy(x => x.Name).ToList(),
            Methods = GetMethodDefinitions(type).DistinctBy(x => x.Name).ToList()
        };

        return typeDefinition;
    }

    private IEnumerable<FunctionDefinition> GetMethodDefinitions(Type type)
    {
#pragma warning disable IL2070
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(x => !x.IsSpecialName).ToList();
#pragma warning restore IL2070

        foreach (var method in methods)
        {
            yield return new FunctionDefinition
            {
                Name = method.Name,
                Parameters = GetMethodParameters(method).ToList(),
                ReturnType = _typeAliasRegistry.TryGetAlias(method.ReturnType, out var alias) ? alias : "any"
            };
        }
    }

    private IEnumerable<ParameterDefinition> GetMethodParameters(MethodInfo method)
    {
        var parameters = method.GetParameters();

        foreach (var parameter in parameters)
        {
            yield return new ParameterDefinition
            {
                Name = parameter.Name!,
                Type = _typeAliasRegistry.TryGetAlias(parameter.ParameterType, out var alias) ? alias : "any",
                IsOptional = parameter.IsOptional
            };
        }
    }

    private IEnumerable<PropertyDefinition> GetPropertyDefinitions([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            yield return new PropertyDefinition
            {
                Name = property.Name,
                Type = _typeAliasRegistry.TryGetAlias(property.PropertyType, out var alias) ? alias : "any",
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