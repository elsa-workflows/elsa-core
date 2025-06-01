using System.Reflection;
using Elsa.Extensions;

namespace Elsa.Workflows.UIHints.Dropdown;

/// <summary>
/// 
/// </summary>
public abstract class DropDownOptionsProviderBase : IPropertyUIHandler
{
    protected virtual bool RefreshOnChange => false;

    public float Priority { get; }

    /// <inheritdoc />
    public virtual async ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
    {
        var selectListItems = await GetItemsAsync(propertyInfo, context, cancellationToken);
        var props = new DropDownProps
        {
            SelectList = new(selectListItems)
        };

        var options = new Dictionary<string, object>
        {
            [InputUIHints.DropDown] = props
        };

        options.AddRange(GetUIPropertyAdditionalOptions());

        return options;
    }

    /// <summary>
    /// Implement this to provide items to the dropdown list.
    /// </summary>
    protected abstract ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken);

    /// <summary>
    /// Override this to provide additional options to the list
    /// </summary>
    /// <returns></returns>
    protected virtual IDictionary<string, object> GetUIPropertyAdditionalOptions()
    {
        var options = new Dictionary<string, object>();
        if (RefreshOnChange) options["Refresh"] = true;
        return options;
    }
}