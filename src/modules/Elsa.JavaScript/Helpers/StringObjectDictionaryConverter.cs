using System.Collections;

namespace Elsa.JavaScript.Helpers;

/// <summary>
/// Contains methods for converting dictionaries with string keys and object values by replacing IList fields with Array fields.
/// </summary>
public static class StringObjectDictionaryConverter
{
    /// <summary>
    /// Recursively converts all IList fields of an ExpandoObject to Array fields.
    /// This allows JS expressions to properly use Array methods on lists, such as .length, filter, etc.
    /// </summary>
    public static object? ConvertListsToArray(object? value)
    {
        if (value is not IDictionary<string, object> dictionary) 
            return value;

        // Copy the dictionary to avoid modifying the original.
        dictionary = new Dictionary<string, object>(dictionary);
        var keys = dictionary.Keys.ToList();
        foreach (var key in keys)
        {
            if (dictionary[key] is IList && dictionary[key].GetType().IsGenericType)
            {
                var list = (IList)dictionary[key];
                var elementType = dictionary[key].GetType().GetGenericArguments()[0];
                var array = Array.CreateInstance(elementType, list.Count);
                list.CopyTo(array, 0);
                dictionary[key] = array;
            }
            else
            {
                ConvertListsToArray(dictionary[key]);
            }
        }

        return dictionary;
    }
}