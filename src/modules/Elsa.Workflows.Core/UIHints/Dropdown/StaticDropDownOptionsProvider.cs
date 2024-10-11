using System.Reflection;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.UIHints.Dropdown;

/// <summary>
/// Provides static drop-down options for a given property.
/// </summary>
public class StaticDropDownOptionsProvider : IPropertyUIHandler
{
    /// <inheritdoc />
    public ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
    {
        var inputAttribute = propertyInfo.GetCustomAttribute<InputAttribute>();
        var inputOptions = inputAttribute?.Options;
        var dictionary = new Dictionary<string, object>();
        var isNullableEnum = false;

        if (inputOptions == null)
        {
            // Is the property an enum?
            var wrappedPropertyType = propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Input<>)
                ? propertyInfo.PropertyType.GetGenericArguments()[0]
                : propertyInfo.PropertyType;

            if (!wrappedPropertyType.IsEnum) {

                // Is the property a nullable enum?
                var nullablePropertyType = Nullable.GetUnderlyingType(wrappedPropertyType);
                if (nullablePropertyType == null || !nullablePropertyType.IsEnum) {
                    return new(dictionary);
                }
                else {
                    wrappedPropertyType = nullablePropertyType;
                    isNullableEnum = true;
                }
            }

            var enumValues = Enum.GetValues(wrappedPropertyType).Cast<object>().ToList();
            var enumSelectListItems = enumValues.Select(x => new SelectListItem(x.ToString()!, x.ToString()!)).ToList();
            if (isNullableEnum)
                enumSelectListItems.Insert(0, new SelectListItem("-",""));
            var enumProps = new DropDownProps
            {
                SelectList = new SelectList(enumSelectListItems)
            };

            dictionary[InputUIHints.DropDown] = enumProps;
            return new(dictionary);
        }

        var selectListItems = (inputOptions as ICollection<string>)?.Select(x => new SelectListItem(x, x)).ToList();

        if (selectListItems == null)
            return new(dictionary);

        var props = new DropDownProps
        {
            SelectList = new SelectList(selectListItems)
        };

        dictionary[InputUIHints.DropDown] = props;
        return new(dictionary);
    }
}