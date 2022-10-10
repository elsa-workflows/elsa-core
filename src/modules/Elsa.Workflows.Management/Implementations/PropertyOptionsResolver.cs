using System.Reflection;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Implementations;

public class PropertyOptionsResolver : IPropertyOptionsResolver
{
    private readonly IServiceProvider _serviceProvider;

    public PropertyOptionsResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object? GetOptions(PropertyInfo propertyInfo)
    {
        var inputAttribute = propertyInfo.GetCustomAttribute<InputAttribute>();

        if (inputAttribute == null)
            return null;

        if (inputAttribute.OptionsProvider == null)
            return inputAttribute.Options ?? (TryGetEnumOptions(propertyInfo, out var items) ? items : null);

        var providerType = inputAttribute.OptionsProvider;

        using var scope = _serviceProvider.CreateScope();
        var provider = (IActivityPropertyOptionsProvider) ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
        return provider.GetOptions(propertyInfo);
    }

    private bool TryGetEnumOptions(PropertyInfo activityPropertyInfo, out IList<SelectListItem>? items)
    {
        var isNullable = activityPropertyInfo.PropertyType.IsNullableType();
        var propertyType = isNullable ? activityPropertyInfo.PropertyType.GetTypeOfNullable() : activityPropertyInfo.PropertyType;
        var wrappedPropertyType = !typeof(Input).IsAssignableFrom(propertyType) ? propertyType : activityPropertyInfo.PropertyType.GenericTypeArguments[0];

        items = null;

        if (!wrappedPropertyType.IsEnum)
            return false;
            
        items = wrappedPropertyType.GetEnumNames().Select(x => new SelectListItem(x.Humanize(LetterCasing.Title), x)).ToList();

        if (isNullable)
            items.Insert(0, new SelectListItem("-", ""));

        return true;
    }
}