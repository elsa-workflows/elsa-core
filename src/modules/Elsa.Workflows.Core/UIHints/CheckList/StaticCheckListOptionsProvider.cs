using System.Reflection;
using Elsa.Workflows.Attributes;

namespace Elsa.Workflows.UIHints.CheckList;

/// <summary>
/// Provides static drop-down options for a given property.
/// </summary>
public class StaticCheckListOptionsProvider : IPropertyUIHandler
{
    /// <inheritdoc />
    public ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
    {
        var inputAttribute = propertyInfo.GetCustomAttribute<InputAttribute>();
        var inputOptions = inputAttribute?.Options;
        var dictionary = new Dictionary<string, object>();

        if (inputOptions == null)
            return new(dictionary);

        var selectListItems = (inputOptions as ICollection<string>)?.Select(x => new CheckListItem(x, x)).ToList();

        if (selectListItems == null)
            return new(dictionary);

        var props = new CheckListProps
        {
            CheckList = new CheckList
            {
                Items = selectListItems.ToList() 
            }
        };

        dictionary[InputUIHints.CheckList] = props;
        return new(dictionary);
    }
}