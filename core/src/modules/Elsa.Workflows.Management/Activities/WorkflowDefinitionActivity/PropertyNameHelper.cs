namespace Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;

internal static class PropertyNameHelper
{
    public static string GetSafePropertyName(Type type, string name)
    {
        var isReservedName = IsReservedName(type, name);
        return isReservedName ? PrefixPropertyName(name) : name;
    }
    
    public static string GetUnsafePropertyName(Type type, string name)
    {
        var unsafeName = RemovePropertyNamePrefix(name);
        var isReservedName = IsReservedName(type, unsafeName);
        return isReservedName ? unsafeName : name;
    }

    private static string PrefixPropertyName(string name) => $"_{name}";
    private static string RemovePropertyNamePrefix(string name) => name.TrimStart('_');
    private static bool IsReservedName(Type type, string name) => type.GetProperty(name) != null;
}