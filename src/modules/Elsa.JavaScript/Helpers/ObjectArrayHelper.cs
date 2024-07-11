using System.Collections;

namespace Elsa.JavaScript.Helpers;

/// <summary>
/// Contains helper methods for working with object arrays.
/// </summary>
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