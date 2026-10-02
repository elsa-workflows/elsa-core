using System.Reflection;
using Elsa.Workflows.UIHints.CheckList;

namespace Elsa.Server.Web;

/// <summary>
/// Provides static drop-down options for a given property.
/// </summary>
public class CustomCheckListOptionsProvider : CheckListOptionsProviderBase
{
    /// <inheritdoc />
    protected override ValueTask<ICollection<CheckListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var items = new[]
        {
            "Apple", "Banana", "Cherry"
        };
        var selectListItems = items.Select(x => new CheckListItem(x, x)).ToList();

        return new(selectListItems);
    }
}