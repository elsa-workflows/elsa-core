using System.Collections;

namespace Elsa.Workflows.Helpers;

/// <summary>
/// Detects and reads properties that hold a collection of inputs, or a dictionary of inputs or outputs with string keys.
/// It works through the generic <see cref="ICollection{T}"/> and <see cref="IDictionary{TKey,TValue}"/> interfaces, so arrays, sets,
/// subclasses and other implementations are supported, not only types that also implement the non-generic interfaces.
/// </summary>
internal static class InputOutputCollections
{
    /// <summary>
    /// Returns the item type if the specified type is a collection whose items are assignable to <paramref name="itemBaseType"/>; otherwise, <c>null</c>.
    /// </summary>
    public static Type? GetCollectionItemType(Type type, Type itemBaseType) =>
        GetGenericInterfaces(type, typeof(ICollection<>))
            .Select(x => x.GenericTypeArguments[0])
            .FirstOrDefault(itemBaseType.IsAssignableFrom);

    /// <summary>
    /// Returns the value type if the specified type is a dictionary with string keys whose values are assignable to <paramref name="valueBaseType"/>; otherwise, <c>null</c>.
    /// </summary>
    public static Type? GetDictionaryValueType(Type type, Type valueBaseType) =>
        GetGenericInterfaces(type, typeof(IDictionary<,>))
            .Where(x => x.GenericTypeArguments[0] == typeof(string))
            .Select(x => x.GenericTypeArguments[1])
            .FirstOrDefault(valueBaseType.IsAssignableFrom);

    /// <summary>
    /// Returns the items of a collection of <typeparamref name="T"/>, or <c>null</c> if the value isn't one.
    /// </summary>
    public static IReadOnlyList<T?>? ReadCollection<T>(object? value) where T : class =>
        value is IEnumerable<T?> items ? items.ToList() : null;

    /// <summary>
    /// Returns the entries of a dictionary with string keys and <typeparamref name="T"/> values, in enumeration order, or <c>null</c> if the value isn't one.
    /// </summary>
    public static IReadOnlyDictionary<string, T?>? ReadDictionary<T>(object? value) where T : class
    {
        if (value == null)
        {
            return null;
        }

        var valueType = GetDictionaryValueType(value.GetType(), typeof(T));

        if (valueType == null)
        {
            return null;
        }

        var entryType = typeof(KeyValuePair<,>).MakeGenericType(typeof(string), valueType);
        var keyProperty = entryType.GetProperty(nameof(KeyValuePair<string, T>.Key))!;
        var valueProperty = entryType.GetProperty(nameof(KeyValuePair<string, T>.Value))!;
        var entries = new Dictionary<string, T?>();

        foreach (var entry in (IEnumerable)value)
        {
            entries[(string)keyProperty.GetValue(entry)!] = (T?)valueProperty.GetValue(entry);
        }

        return entries;
    }

    /// <summary>
    /// Sets an entry of a dictionary with string keys and <paramref name="valueType"/> values.
    /// </summary>
    public static void SetDictionaryEntry(object dictionary, Type valueType, string key, object? value)
    {
        var dictionaryType = typeof(IDictionary<,>).MakeGenericType(typeof(string), valueType);
        dictionaryType.GetProperty("Item")!.SetValue(dictionary, value, [key]);
    }

    private static IEnumerable<Type> GetGenericInterfaces(Type type, Type genericInterface)
    {
        var interfaces = type.IsInterface ? type.GetInterfaces().Prepend(type) : type.GetInterfaces();
        return interfaces.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == genericInterface);
    }
}
