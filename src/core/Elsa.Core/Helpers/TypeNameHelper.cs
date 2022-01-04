namespace Elsa.Helpers;

public static class TypeNameHelper
{
    private static readonly string[] WellknownNamespaces = { "Elsa.Activities", "Elsa.Triggers" };

    public static string? GenerateNamespace(Type activityType) => GenerateTypeNamespace(activityType);

    public static string GenerateTypeName(Type type, string? ns)
    {
        var typeName = type.Name;
        return ns != null ? $"{ns}.{typeName}" : typeName;
    }

    public static string GenerateTypeName<T>() => GenerateTypeName(typeof(T));

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

    private static string? GenerateTypeNamespace(Type type)
    {
        if (type.Namespace == null)
            return null;

        foreach (var wellknownNamespace in WellknownNamespaces)
            if (type.Namespace.StartsWith(wellknownNamespace))
                return type.Namespace[(wellknownNamespace.Length + 1)..];

        return type.Namespace;
    }
}