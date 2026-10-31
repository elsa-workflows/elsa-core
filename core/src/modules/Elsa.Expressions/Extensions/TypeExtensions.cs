using System.Diagnostics.CodeAnalysis;
using System.Text;
using Elsa.Expressions.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="Type"/>.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Returns the default value for the specified type.
    /// </summary>
    public static object? GetDefaultValue(this Type type) => type.IsClass ? null : Activator.CreateInstance(type);
    
    /// <summary>
    /// Returns the element type of the specified type representing an array or generic enumerable.
    /// </summary>
    public static Type GetEnumerableElementType(this Type type)
    {
        if (type.IsArray)
            return type.GetElementType()!;

        var elementType = FindIEnumerable(type);
        return elementType == null ? type : elementType.GetGenericArguments()[0];
    }

    /// <summary>
    /// Searches for the first implemented IEnumerable interface in the given type hierarchy, and returns the generic type argument of the interface. 
    /// </summary>
    /// <param name="sequenceType">The type to search for the IEnumerable interface.</param>
    /// <returns>The generic type argument of the first implemented IEnumerable interface found in the type hierarchy, or null if none is found.</returns>
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    private static Type? FindIEnumerable([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type? sequenceType)
    {
        if (sequenceType == null || sequenceType == typeof(string))
            return null;
        
        if (sequenceType.IsArray)
            return typeof(IEnumerable<>).MakeGenericType(sequenceType.GetElementType()!);
        
        if (sequenceType.IsGenericType)
        {
            foreach (var arg in sequenceType.GetGenericArguments())
            {
                var enumerable = typeof(IEnumerable<>).MakeGenericType(arg);
                if (enumerable.IsAssignableFrom(sequenceType))
                    return enumerable;
            }
        }
        
        var interfaces = sequenceType.GetInterfaces();
        
        if (interfaces is { Length: > 0 })
        {
            foreach (var interfaceType in interfaces)
            {
                var enumerable = FindIEnumerable(interfaceType);
                if (enumerable != null) return enumerable;
            }
        }
        if (sequenceType.BaseType != null && sequenceType.BaseType != typeof(object))
            return FindIEnumerable(sequenceType.BaseType);
        
        return null;
    }
    
    /// <summary>
    /// Gets a friendly type name for the specified type.
    /// </summary>
    public static string GetFriendlyTypeName(this Type type, Brackets brackets)
    {
        if (type.IsArray)
        {
            var elementTypeName = GetFriendlyTypeName(type.GetElementType()!, brackets);
            var rank = type.GetArrayRank();
            var commas = rank > 1 ? new string(',', rank - 1) : string.Empty;
            return elementTypeName + "[" + commas + "]";
        }

        if (!type.IsGenericType)
            return type.FullName!;

        var sb = new StringBuilder();
        sb.Append(type.Namespace);
        sb.Append('.');
        sb.Append(type.Name[..type.Name.IndexOf('`')]);
        sb.Append(brackets.Open);
        var genericArgs = type.GetGenericArguments();
        for (var i = 0; i < genericArgs.Length; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append(GetFriendlyTypeName(genericArgs[i], brackets));
        }

        sb.Append(brackets.Close);
        return sb.ToString();
    }

}
