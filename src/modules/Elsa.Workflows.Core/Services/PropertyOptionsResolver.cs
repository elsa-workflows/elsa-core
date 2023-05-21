using System.Reflection;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class PropertyOptionsResolver : IPropertyOptionsResolver
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyOptionsResolver"/> class.
    /// </summary>
    public PropertyOptionsResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async ValueTask<object?> GetOptionsAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken = default)
    {
        var inputAttribute = propertyInfo.GetCustomAttribute<InputAttribute>();
        
        if (inputAttribute?.OptionsProvider == null)
            return inputAttribute?.Options ?? (TryGetEnumOptions(propertyInfo, out var items) ? items : null);

        var providerType = inputAttribute.OptionsProvider;

        using var scope = _serviceProvider.CreateScope();
        var provider = (IActivityPropertyOptionsProvider) ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
        return await provider.GetOptionsAsync(propertyInfo, cancellationToken);
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