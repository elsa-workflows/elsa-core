using System.Reflection;
using Elsa.Workflows.Attributes;

namespace Elsa.Workflows.UIHints.CheckList;

/// <summary>
/// Provides static drop-down options for a given property.
/// </summary>
public class StaticCheckListOptionsProvider : CheckListOptionsProviderBase
{
    public override float Priority => -1;

    /// <inheritdoc />
    protected override ValueTask<ICollection<CheckListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var inputAttribute = propertyInfo.GetCustomAttribute<InputAttribute>();
        var inputOptions = inputAttribute?.Options;

        if (inputOptions == null)
            return new([]);

        var selectListItems = (inputOptions as ICollection<string>)?.Select(x => new CheckListItem(x, x)).ToList();

        if (selectListItems == null)
            return new([]);
        
        return new(selectListItems);
    }
}