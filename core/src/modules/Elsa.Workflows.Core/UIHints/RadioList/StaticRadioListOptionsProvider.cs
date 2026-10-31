using System.Reflection;
using Elsa.Workflows.Attributes;

namespace Elsa.Workflows.UIHints.RadioList;

/// <summary>
/// Provides static drop-down options for a given property.
/// </summary>
public class StaticRadioListOptionsProvider : RadioListOptionsProviderBase
{
    public override float Priority => -1;

    /// <inheritdoc />
    protected override ValueTask<ICollection<RadioListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var inputAttribute = propertyInfo.GetCustomAttribute<InputAttribute>();
        var inputOptions = inputAttribute?.Options;

        if (inputOptions == null)
            return new([]);

        var selectListItems = (inputOptions as ICollection<string>)?.Select(x => new RadioListItem(x, x)).ToList();

        if (selectListItems == null)
            return new([]);
        
        return new(selectListItems);
    }
}