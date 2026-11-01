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
            var declaringType = type.DeclaringType;
            var inheritedGenericArgumentCount = declaringType?.IsGenericType == true
                ? declaringType.GetGenericArguments().Length
                : 0;
            var genericArguments = type.GetGenericArguments().ToArray();
            var inheritedGenericArguments = genericArguments.Take(inheritedGenericArgumentCount).ToArray();
            var ownGenericArguments = genericArguments.Skip(inheritedGenericArgumentCount).ToArray();

            // Only activities nested inside a GENERIC declaring type carry the declaring type name, which keeps them
            // distinct from a same-named top-level activity in the registry. Activities nested inside non-generic
            // types keep their historical type name.
            // The declaring type of a nested type can be reported as its open generic definition, whose arguments are
            // parameter names. When the nested activity is closed over the declaring type's arguments and declares its
            // own, the declaring type is closed with the constructed arguments inherited by the nested type, so
            // different closings of the same nested type keep distinct type names.
            var declaringTypeForPrefix = declaringType is { IsGenericTypeDefinition: true }
                && ownGenericArguments.Length > 0
                && inheritedGenericArguments.All(arg => !arg.IsGenericParameter)
                    ? declaringType.MakeGenericType(inheritedGenericArguments)
                    : declaringType;
            var declaringTypeName = declaringType?.IsGenericType == true
                ? $"{GenerateTypeName(declaringTypeForPrefix, null)}."
                : string.Empty;

            // Nested types of generic types are generic types themselves but do not have their own backtick suffix in their name.
            // Only strip the generic arity suffix when it is present.
            var backtickIndex = type.Name.IndexOf('`');
            var baseTypeName = backtickIndex >= 0
                ? type.Name[..backtickIndex]
                : type.Name;

            var sanitizedTypeName = activityAttr?.Type ?? baseTypeName;

            // GetGenericArguments() works for closed and open generic types, unlike the GenericTypeArguments property,
            // which returns an empty array for open generic definitions. Closed types render their constructed generic
            // arguments and open generic definitions render their generic parameter names; the arguments inherited from
            // the generic declaring type are skipped to avoid duplication while the nested activity's own arguments
            // are preserved.
            var genericArgs = ownGenericArguments.Length > 0
                ? ownGenericArguments
                : genericArguments.Where(arg => !arg.IsGenericParameter).ToArray();

            var genericSuffix = genericArgs.Length > 0
                ? $"<{string.Join(',', genericArgs.Select(arg => GenerateTypeName(arg, null)))}>"
                : string.Empty;

            typeName = $"{declaringTypeName}{sanitizedTypeName}{genericSuffix}";
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