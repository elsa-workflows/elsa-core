using System.Reflection;
using Elsa.Workflows.Attributes;

namespace Elsa.Workflows.UIHints.Dictionary;

/// <summary>
/// A static dictionary options provider that provides an empty dictionary by default.
/// </summary>
public class StaticDictionaryOptionsProvider : DictionaryOptionsProviderBase
{
    /// <inheritdoc />
    protected override ValueTask<ICollection<DictionaryItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        // Return an empty collection by default - the UI will allow users to add items
        var inputAttribute = propertyInfo.GetCustomAttribute<InputAttribute>();
        var inputOptions = inputAttribute?.Options;
        var items = new List<DictionaryItem>();
        return new ValueTask<ICollection<DictionaryItem>>(items);
    }
}
