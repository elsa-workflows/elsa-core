using System.Reflection;
using Elsa.Workflows.Attributes;

namespace Elsa.Workflows.Helpers;

public static class ActivityTypeNameHelper
{
    public static string? GenerateNamespace(Type activityType)
    {
        var activityAttr = activityType.GetCustomAttribute<ActivityAttribute>();
        return activityAttr?.Namespace ?? activityType.Namespace;
    }

    public static string GenerateTypeName(Type type, string? ns)
    {
        var activityAttr = type.GetCustomAttribute<ActivityAttribute>();
        string typeName;
        
        if (type.IsGenericType)
        {
            var backtickIndex = type.Name.IndexOf('`');

            if (backtickIndex < 0 && activityAttr?.Type == null)
            {
                // Nested types of generic types are generic types themselves without a backtick suffix in their own
                // name (#9154), which used to throw an ArgumentOutOfRangeException and therefore has no stored type
                // name depending on it. The nested type name is qualified with its closed declaring type, so the
                // container's general arguments are kept in the name.
                var declaringType = type.DeclaringType!;

                if (type.IsConstructedGenericType)
                    declaringType = declaringType.MakeGenericType(type.GenericTypeArguments);

                typeName = $"{GenerateTypeName(declaringType, null)}.{type.Name}";
            }
            else
            {
                // Preserve the existing naming behavior from main.
                var sanitizedTypeName = activityAttr?.Type ?? type.Name[..backtickIndex];
                var genericArgs = type.GenericTypeArguments.Select(t => GenerateTypeName(t, null));

                typeName = $"{sanitizedTypeName}<{string.Join(',', genericArgs)}>";
            }
        }
        else
            typeName = activityAttr?.Type ?? type.Name;

        return ns != null ? $"{ns}.{typeName}" : typeName;
    }

    public static string GenerateTypeName<T>() where T : IActivity => GenerateTypeName(typeof(T));

    public static string GenerateTypeName(Type type)
    {
        var ns = GenerateNamespace(type);
        return GenerateTypeName(type, ns);
    }

    public static string? GetCategoryFromNamespace(string? ns)
    {
        if (string.IsNullOrWhiteSpace(ns))
            return null;

        var index = ns.LastIndexOf('.');

        return index < 0 ? ns : ns[(index + 1)..];
    }
}