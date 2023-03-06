using System.Reflection;
using Elsa.Jobs.Activities.Attributes;
using Elsa.Jobs.Contracts;

namespace Elsa.Jobs.Activities.Helpers;

public static class JobTypeNameHelper
{
    public static string? GenerateNamespace(Type jobType)
    {
        var activityAttr = jobType.GetCustomAttribute<JobAttribute>();
        return activityAttr?.Namespace ?? jobType.Namespace;
    }

    public static string GenerateTypeName(Type type, string? ns)
    {
        var activityAttr = type.GetCustomAttribute<JobAttribute>();
        var typeName = activityAttr?.ActivityType ?? type.Name;
        return ns != null ? $"{ns}.{typeName}" : typeName;
    }

    public static string GenerateTypeName<T>() where T : IJob => GenerateTypeName(typeof(T));

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