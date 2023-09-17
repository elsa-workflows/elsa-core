using System.Reflection;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Helpers;

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
            var sanitizedTypeName = activityAttr?.Type ?? type.Name.Substring(0, type.Name.IndexOf("`", StringComparison.InvariantCulture));
            var genericArgs = type.GenericTypeArguments.Select(type1 => GenerateTypeName(type1, null));
            typeName = $"{sanitizedTypeName}<{string.Join(',', genericArgs)}>";
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