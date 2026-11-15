using System.Diagnostics.CodeAnalysis;

namespace Elsa.Workflows.Helpers;

/// <summary>
/// Names the entries of dictionary inputs and outputs, such as <c>Dictionary&lt;string, Input&lt;T&gt;&gt;</c> and <c>Dictionary&lt;string, Output&lt;T&gt;&gt;</c>.
/// </summary>
public static class DictionaryEntryNames
{
    /// <summary>
    /// Separates the name of a dictionary input or output from the key of one of its entries.
    /// Keys can't contain it.
    /// </summary>
    public const char Separator = ':';

    /// <summary>
    /// Returns the name under which an entry of a dictionary output is recorded in the activity output register, for example <c>Outputs:name</c>.
    /// Prefixing the key with the output name keeps the entries apart from the activity's other outputs and from the entries of its other dictionary outputs.
    /// </summary>
    /// <param name="outputName">The name of the dictionary output.</param>
    /// <param name="key">The key of the entry.</param>
    public static string GetOutputName(string outputName, string key) => $"{outputName}{Separator}{key}";

    /// <summary>
    /// Returns <c>true</c> if the specified key can be used for an entry of a dictionary input or output.
    /// A key must not be empty or white space, and must not contain <see cref="Separator"/>.
    /// </summary>
    public static bool IsValidKey([NotNullWhen(true)] string? key) => !string.IsNullOrWhiteSpace(key) && !key.Contains(Separator);

    internal static void EnsureValidKey(string? key, string activityId, string propertyName)
    {
        if (!IsValidKey(key))
        {
            throw new InvalidOperationException($"Activity '{activityId}' has an entry with key '{key}' in '{propertyName}'. Keys of dictionary inputs and outputs must not be empty and must not contain '{Separator}'.");
        }
    }
}
