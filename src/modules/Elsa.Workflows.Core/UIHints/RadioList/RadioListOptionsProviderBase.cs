using System.Reflection;
using Elsa.Extensions;

namespace Elsa.Workflows.UIHints.RadioList;

/// <summary>
/// A base class for providing options to populate a checklist UI component. This class is intended to be inherited to implement
/// custom radiolist data logic by overriding the `GetItemsAsync` method.
/// </summary>
public abstract class RadioListOptionsProviderBase : PropertyUIHandlerBase
{
    protected virtual bool RefreshOnChange => false;

    /// <inheritdoc />
    public override async ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
    {
        var items = await GetItemsAsync(propertyInfo, context, cancellationToken);
        var props = new RadioListProps
        {
            RadioList = new()
            {
                Items = items.ToList()
            }
        };

        var options = new Dictionary<string, object>
        {
            [InputUIHints.RadioList] = props
        };

        options.AddRange(GetUIPropertyAdditionalOptions());

        return options;
    }

    /// <summary>
    /// Implement this to provide items to the radio list.
    /// </summary>
    protected abstract ValueTask<ICollection<RadioListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken);

    protected virtual IDictionary<string, object> GetUIPropertyAdditionalOptions()
    {
        var options = new Dictionary<string, object>();
        if (RefreshOnChange) options["Refresh"] = true;
        return options;
    }
}