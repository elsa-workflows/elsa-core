namespace Elsa.Workflows.ComponentTests;

public static class DictionaryExtensions
{
    public static T GetOrAdd<T>(this IDictionary<string, T> dictionary, string key, Func<T> factory)
    {
        if (dictionary.TryGetValue(key, out var value))
            return value;

        var newValue = factory();
        dictionary[key] = newValue!;
        return newValue;
    }
}