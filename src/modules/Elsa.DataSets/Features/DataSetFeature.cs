using Elsa.Common.Services;
using Elsa.DataSets.Contracts;
using Elsa.DataSets.Entities;
using Elsa.DataSets.Stores;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.DataSets.Features;

/// <summary>
/// Feature for data sets.
/// </summary>
public class DataSetFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, IDataSetDefinitionStore> _dataSetStoreFactory = sp => sp.GetRequiredService<MemoryDataSetDefinitionStore>();
    private Func<IServiceProvider, ILinkedServiceDefinitionStore> _linkedServiceDefinitionStore = sp => sp.GetRequiredService<MemoryLinkedServiceDefinitionStore>();
    
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

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton<MemoryStore<DataSetDefinition>>();
        Services.AddSingleton<MemoryStore<LinkedServiceDefinition>>();
        Services.AddSingleton<MemoryDataSetDefinitionStore>();
        Services.AddSingleton<MemoryLinkedServiceDefinitionStore>();
        base.Apply();
    }
}