using System.Reflection;

namespace Elsa.Extensions;

public static class ObjectExtensions
{
    public static IDictionary<string, object> ToDictionary(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance) => source.ToDictionaryInternal(bindingAttr);
    public static IReadOnlyDictionary<string, object> ToReadOnlyDictionary(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance) => source.ToDictionaryInternal(bindingAttr);

    private static Dictionary<string, object> ToDictionaryInternal(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance) =>
        source.GetType().GetProperties(bindingAttr).ToDictionary
        (
            propInfo => propInfo.Name,
            propInfo => propInfo.GetValue(source, null)!
        );
}