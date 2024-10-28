using System.Reflection;

namespace Elsa.Workflows.UIHints.Dropdown;

/// <summary>
/// 
/// </summary>
public abstract class DropDownOptionsProviderBase : IPropertyUIHandler
{
    /// <inheritdoc />
    public async ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
    {
        var selectListItems = await GetItemsAsync(propertyInfo, context, cancellationToken);
        var props = new DropDownProps
        {
            SelectList = new SelectList(selectListItems)
        };

        var options = new Dictionary<string, object>
        {
            [InputUIHints.DropDown] = props
        };

        return options;
    }

    /// <summary>
    /// Implement this to provide items to the dropdown list.
    /// </summary>
    protected abstract ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken);
}