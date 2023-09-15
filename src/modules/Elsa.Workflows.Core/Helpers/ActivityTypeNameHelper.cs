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
        if (activityAttr?.Type is null)
        {

            if (type.IsGenericType)
            {
                var genericArgs = type.GenericTypeArguments.Select(type1 => GenerateTypeName(type1, null));
                typeName = $"{type.Name.Substring(0, type.Name.IndexOf("`", StringComparison.InvariantCulture))}<{string.Join(',', genericArgs)}>";
            }
            else
                typeName = type.Name;
        }
        else 
            typeName = activityAttr.Type;

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