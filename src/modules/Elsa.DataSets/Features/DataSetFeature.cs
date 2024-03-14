using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Elsa.Common.Services;
using Elsa.DataSets.Contracts;
using Elsa.DataSets.Entities;
using Elsa.DataSets.Options;
using Elsa.DataSets.Providers;
using Elsa.DataSets.Serialization;
using Elsa.DataSets.Stores;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.DataSets.Features;

/// <summary>
/// Feature for data sets.
/// </summary>
public class DataSetFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, IDataSetDefinitionStore> _dataSetStoreFactory = sp => sp.GetRequiredService<MemoryDataSetDefinitionStore>();
    private Func<IServiceProvider, IDataSetDefinitionProvider> _dataSetDefinitionProviderFactory = sp => sp.GetRequiredService<StoreBasedDataSetDefinitionProvider>();
    private Func<IServiceProvider, ILinkedServiceDefinitionStore> _linkedServiceDefinitionStore = sp => sp.GetRequiredService<MemoryLinkedServiceDefinitionStore>();
    private Func<IServiceProvider, ILinkedServiceDefinitionProvider> _linkedServiceDefinitionProviderFactory = sp => sp.GetRequiredService<StoreBasedLinkedServiceDefinitionProvider>();
    private Action<DataSetOptions> _configureDataSetOptions = _ => { };

    /// <summary>
    /// Configures the feature to use the specified data set store.
    /// </summary>
    public DataSetFeature WithDataSetStore(Func<IServiceProvider, IDataSetDefinitionStore> dataSetStoreFactory)
    {
        _dataSetStoreFactory = dataSetStoreFactory;
        return this;
    }

    /// <summary>
    /// Configures the feature to use the specified linked service definition store.
    /// </summary>
    public DataSetFeature WithLinkedServiceDefinitionStore(Func<IServiceProvider, ILinkedServiceDefinitionStore> linkedServiceDefinitionStore)
    {
        _linkedServiceDefinitionStore = linkedServiceDefinitionStore;
        return this;
    }

    /// <summary>
    /// Configures the feature to use the specified data set definition provider.
    /// </summary>
    public DataSetFeature WithDataSetDefinitionProvider(Func<IServiceProvider, IDataSetDefinitionProvider> dataSetDefinitionProviderFactory)
    {
        _dataSetDefinitionProviderFactory = dataSetDefinitionProviderFactory;
        return this;
    }

    /// <summary>
    /// Configures the feature to use the specified linked service definition provider.
    /// </summary>
    /// <param name="linkedServiceDefinitionProviderFactory"></param>
    /// <returns></returns>
    public DataSetFeature WithLinkedServiceDefinitionProvider(Func<IServiceProvider, ILinkedServiceDefinitionProvider> linkedServiceDefinitionProviderFactory)
    {
        _linkedServiceDefinitionProviderFactory = linkedServiceDefinitionProviderFactory;
        return this;
    }

    /// <summary>
    /// Configures the feature to use configuration-based data set and linked service definition providers.
    /// </summary>
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    public DataSetFeature WithConfigurationBasedProviders(IConfigurationSection configurationSection)
    {
        ConfigureOptions(options => BindOptions(configurationSection, options));
        _dataSetDefinitionProviderFactory = sp => sp.GetRequiredService<ConfigurationBasedDataSetDefinitionProvider>();
        _linkedServiceDefinitionProviderFactory = sp => sp.GetRequiredService<ConfigurationBasedLinkedServiceDefinitionProvider>();
        return this;
    }

    /// <summary>
    /// Configures the <see cref="DataSetOptions"/>
    /// </summary>
    public DataSetFeature ConfigureOptions(Action<DataSetOptions> configure)
    {
        _configureDataSetOptions += configure;
        return this;
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(_configureDataSetOptions);
        Services.AddSingleton<MemoryStore<DataSetDefinition>>();
        Services.AddSingleton<MemoryStore<LinkedServiceDefinition>>();
        Services.AddSingleton<MemoryDataSetDefinitionStore>();
        Services.AddSingleton<MemoryLinkedServiceDefinitionStore>();
        Services.AddSingleton<StoreBasedDataSetDefinitionProvider>();
        Services.AddSingleton<StoreBasedLinkedServiceDefinitionProvider>();
        Services.AddSingleton<ConfigurationBasedDataSetDefinitionProvider>();
        Services.AddSingleton<ConfigurationBasedLinkedServiceDefinitionProvider>();
        Services.AddSingleton(_dataSetStoreFactory);
        Services.AddSingleton(_linkedServiceDefinitionStore);
        Services.AddSingleton(_dataSetDefinitionProviderFactory);
        Services.AddSingleton(_linkedServiceDefinitionProviderFactory);
        base.Apply();
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    private void BindOptions(IConfigurationSection configurationSection, DataSetOptions options)
    {
        var dataSetsSection = configurationSection.GetSection("DataSets");
        var dataSetsJson = dataSetsSection.Value!;
        var linkedServicesSection = configurationSection.GetSection("LinkedServices");
        var linkedServicesJson = linkedServicesSection.Value!;
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        serializerOptions.Converters.Add(new DataSetDefinitionJsonConverter());
        serializerOptions.Converters.Add(new LinkedServiceDefinitionJsonConverter());

        var dataSets = JsonSerializer.Deserialize<DataSetDefinition[]>(dataSetsJson, serializerOptions)!;
        var linkedServices = JsonSerializer.Deserialize<LinkedServiceDefinition[]>(linkedServicesJson, serializerOptions)!;

        options.DataSetDefinitions.AddRange(dataSets);
        options.LinkedServiceDefinitions.AddRange(linkedServices);
    }
}