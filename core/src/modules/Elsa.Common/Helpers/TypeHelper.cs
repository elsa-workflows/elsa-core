using System.Reflection;

namespace Elsa.Common.Helpers;

public static class TypeHelper
{
    public static Type GetLatestType(string typeName)
    {
        var type = Type.GetType(typeName);
        var deprecatedByTypeAttribute = type?.GetCustomAttribute<ForwardedTypeAttribute>();
        return deprecatedByTypeAttribute?.NewType ?? type ?? throw new InvalidOperationException($"Type {typeName} not found.");
    }
}