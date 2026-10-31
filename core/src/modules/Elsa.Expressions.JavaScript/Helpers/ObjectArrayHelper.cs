using System.Collections;

namespace Elsa.Expressions.JavaScript.Helpers;

/// <summary>
/// Contains helper methods for working with object arrays.
/// </summary>
[Obsolete("Jint decides array-likeness itself and attaches Array.prototype to array-like wrappers when Options.Interop.AttachArrayPrototype is enabled (the default). This helper is no longer used and will be removed in a future version.")]
public static class ObjectArrayHelper
{
    /// <summary>
    /// Determines if the specified object is an array-like CLR collection.
    /// </summary>
    public static bool DetermineIfObjectIsArrayLikeClrCollection(Type type)
    {
        var isDictionary = typeof(IDictionary).IsAssignableFrom(type);
        
        if (isDictionary)
            return false;
        
        if (typeof(ICollection).IsAssignableFrom(type))
            return true;
        
        foreach (var interfaceType in type.GetInterfaces())
        {
            if (!interfaceType.IsGenericType)
            {
                continue;
            }

            if (interfaceType.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)
                || interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>))
            {
                return true;
            }
        }

        return false;
    }
}