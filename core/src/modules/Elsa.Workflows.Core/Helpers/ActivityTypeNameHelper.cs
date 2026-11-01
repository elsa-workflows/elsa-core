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
            // Nested types of generic types are generic types themselves but do not have their own backtick suffix in their name.
            // Only strip the generic arity suffix when it is present.
            var backtickIndex = type.Name.IndexOf("`", StringComparison.InvariantCulture);
            var baseTypeName = backtickIndex >= 0 ? type.Name.Substring(0, backtickIndex) : type.Name;
            var sanitizedTypeName = activityAttr?.Type ?? baseTypeName;
            var declaringTypeName = type.DeclaringType is null
                ? string.Empty
                : $"{GenerateTypeName(type.DeclaringType, null)}.";
            // Generic arguments of a nested type start with the arguments inherited from its declaring type;
            // only the arguments beyond those belong to the nested type itself, which avoids duplication.
            var declaringArgCount = type.DeclaringType?.GetGenericArguments().Length ?? 0;
            var ownArgs = type.GetGenericArguments().Skip(declaringArgCount).ToArray();
            var ownSuffix = ownArgs.Length == 0
                ? string.Empty
                : $"<{string.Join(',', ownArgs.Select(type1 => GenerateTypeName(type1, null)))}>";
            typeName = $"{declaringTypeName}{sanitizedTypeName}{ownSuffix}";
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