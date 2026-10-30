using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Elsa.Studio.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="object"/> class.
/// </summary>
public static class ObjectExtension
{
    /// <summary>
    /// Converts the specified object to a dictionary.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "This helper intentionally reflects over anonymous UI attribute bags.")]
    public static Dictionary<string, object?> ToDictionary(this object obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        var dictionary = new Dictionary<string, object?>();
        foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead)
                continue;

            var value = property.GetValue(obj);
            dictionary.Add(property.Name, value);
        }

        return dictionary;
    }
}
