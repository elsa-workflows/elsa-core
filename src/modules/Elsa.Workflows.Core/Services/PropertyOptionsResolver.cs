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
    public async ValueTask<IDictionary<string, object>?> GetOptionsAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken = default)
    {
        var inputAttribute = propertyInfo.GetCustomAttribute<InputAttribute>();

        if (inputAttribute?.OptionsProvider != null)
        {
            var providerType = inputAttribute.OptionsProvider;

            using var scope = _serviceProvider.CreateScope();
            var provider = (IActivityPropertyOptionsProvider)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
            return await provider.GetOptionsAsync(propertyInfo, cancellationToken);
        }

        if (inputAttribute?.OptionsMethod is not null)
        {
            var activityType = propertyInfo.DeclaringType!;
            var methodName = inputAttribute.OptionsMethod!;
            var method = activityType.GetMethod(methodName, bindingAttr: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (method is null)
                throw new InvalidOperationException($"Could not find static method '{methodName}' on type '{activityType}'.");

            var optionsTask = (ValueTask<IDictionary<string, object>>)method.Invoke(null, new object[] { propertyInfo, cancellationToken })!;
            var options = await optionsTask;
            return options;
        }

        var defaultOptions = inputAttribute?.Options ?? (TryGetEnumOptions(propertyInfo, out var items) ? items : null);
        return defaultOptions != null ? new Dictionary<string, object> { ["items"] = defaultOptions } : null;
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