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
    public async ValueTask<OptionsProviderResult?> GetOptionsAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken = default)
    {
        return await GetOptionsAsync(propertyInfo, null, cancellationToken);       
    }


    //TODO: Return a new model with {MetadataProvider: {} , OptionsItems :{}}
    //Refresh Always, When PropertyChanged, static ?
    /// <inheritdoc />
    public async ValueTask<OptionsProviderResult?> GetOptionsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
    {
        var inputAttribute = propertyInfo.GetCustomAttribute<InputAttribute>();
        var optionsProviderResult = new OptionsProviderResult();
        if (inputAttribute?.OptionsProvider != null)
        {
            var providerType = inputAttribute.OptionsProvider;

            using var scope = _serviceProvider.CreateScope();
            var provider = (IActivityPropertyOptionsProvider)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);

            optionsProviderResult.OptionsItems = await provider.GetOptionsAsync(propertyInfo,context,  cancellationToken);
            optionsProviderResult.ProviderMetadata.Add("isRefreshable", provider.isRefreashable);
            return optionsProviderResult;
        }

        if (inputAttribute?.OptionsMethod is not null)
        {
            var activityType = propertyInfo.DeclaringType!;
            var methodName = inputAttribute.OptionsMethod!;
            var method = activityType.GetMethod(methodName, bindingAttr: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (method is null)
                throw new InvalidOperationException($"Could not find static method '{methodName}' on type '{activityType}'.");

            var optionsTask = (ValueTask<IDictionary<string, object>>)method.Invoke(null, new object[] { propertyInfo, cancellationToken })!;
            optionsProviderResult.OptionsItems = await optionsTask;
            optionsProviderResult.ProviderMetadata.Add("isRefreshable", false);
            return optionsProviderResult;
        }

        var defaultOptions = inputAttribute?.Options ?? (TryGetEnumOptions(propertyInfo, out var items) ? items : null);
        optionsProviderResult.OptionsItems = defaultOptions != null ? new Dictionary<string, object> { ["items"] = defaultOptions } : null;
        optionsProviderResult.ProviderMetadata.Add("isRefreshable", false);

        return optionsProviderResult;
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