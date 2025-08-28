using System.Collections;
using System.Reflection;

namespace Elsa.Logging.Helpers;

public static class LogArgumentHelper
{
    public static object[] ToArgumentsArray(object? input)
    {
        switch (input)
        {
            case null:
                return [];
            case object[] objArray:
                return objArray;
            case IDictionary dict when !dict.GetType().IsGenericType:
                // Only use ConvertDictionaryToPairs for non-generic IDictionary
                return ConvertDictionaryToPairs(dict);
        }
        
        if (TryAsGenericDictionary(input, out var fromGenericDict))
            return fromGenericDict;

        // IEnumerable of KeyValuePair<,>
        if (TryAsKeyValuePairEnumerable(input, out var fromKvpEnumerable))
            return fromKvpEnumerable;

        // Any IEnumerable (but not string or byte[])
        if (input is IEnumerable enumerable and not string and not byte[])
            return EnumerableToArray(enumerable);

        // Prevent treating string as object with properties
        if (input is string)
            return [input];

        // Plain/anonymous object → public readable properties to name/value pairs
        return MapObjectToPropertyPairs(input);
    }

    private static object[] ConvertDictionaryToPairs(IDictionary dict)
    {
        var pairs = new List<object>(dict.Count);
        pairs.AddRange((from DictionaryEntry entry in dict select new KeyValuePair<object?, object?>(entry.Key, entry.Value)).Cast<object>());
        return pairs.ToArray();
    }

    private static bool TryAsGenericDictionary(object input, out object[] pairs)
    {
        var t = input.GetType();

        // Find IDictionary<,> or IReadOnlyDictionary<,> among implemented interfaces
        var dictionaryInterface = t.GetInterfaces()
            .Concat([t])
            .FirstOrDefault(i =>
                i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                 i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));

        if (dictionaryInterface is null)
        {
            pairs = [];
            return false;
        }

        var keyProp = dictionaryInterface.GetProperty("Keys")!;
        var idxProp = dictionaryInterface.GetProperty("Item")!; // this[TKey key]
        var keys = (IEnumerable)keyProp.GetValue(input)!;

        pairs = (from object? key in keys let value = idxProp.GetValue(input, [key]) select new KeyValuePair<object?, object?>(key, value)).Cast<object>().ToArray();
        return true;
    }

    private static bool TryAsKeyValuePairEnumerable(object input, out object[] pairs)
    {
        var t = input.GetType();

        // Look for IEnumerable<T> where T is KeyValuePair<,>
        var elementInterface = t.GetInterfaces()
            .Concat([t])
            .FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                IsKeyValuePair(i.GetGenericArguments()[0]));

        if (elementInterface is null)
        {
            pairs = [];
            return false;
        }

        // We can safely enumerate via non-generic IEnumerable
        var list = new List<object>();
        foreach (var item in (IEnumerable)input)
        {
            // Item is a KeyValuePair<,> boxed as object; get Key/Value via reflection
            var it = item.GetType();
            var key = it.GetProperty("Key")!.GetValue(item);
            var value = it.GetProperty("Value")!.GetValue(item);
            list.Add(new KeyValuePair<object?, object?>(key, value));
        }

        pairs = list.ToArray();
        return true;
    }

    private static bool IsKeyValuePair(Type t)
    {
        return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
    }

    private static object[] EnumerableToArray(IEnumerable enumerable)
    {
        return enumerable.Cast<object>().ToArray();
    }

    private static object[] MapObjectToPropertyPairs(object input)
    {
        // Handles regular objects and anonymous types (public get-only props)
        var props = input.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        return props.Select(prop => new KeyValuePair<string, object?>(prop.Name, prop.GetValue(input))).Cast<object>().ToArray();
    }
}