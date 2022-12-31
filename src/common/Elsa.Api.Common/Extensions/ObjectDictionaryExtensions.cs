// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

static class ObjectDictionaryExtensions
{
    public static T GetOrAdd<T>(this IDictionary<object, object> dictionary, object key, Func<T> valueFactory)
    {
        if (dictionary.TryGetValue(key, out var value)) return (T)value;
        value = valueFactory();
        dictionary.Add(key, value!);
        return (T)value!;
    }
    
    public static T Get<T>(this IDictionary<object, object> dictionary, object key) => (T)dictionary[key];
}