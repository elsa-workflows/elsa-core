using Elsa.DataSets.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.DataSets.Models;

/// <summary>
/// Represents a reference to a dataset.
/// </summary>
public class DataSetReference : IDataSource
{
    /// <summary>
    /// Creates a new <see cref="DataSetReference"/> instance.
    /// </summary>
    public static DataSetReference Create(string name) => new() { Name = name };
    
    /// <summary>
    /// Gets or sets the name of the dataset reference.
    /// </summary>
    public string Name { get; set; }
    
    public async IAsyncEnumerable<T> ListAsync<T>(DataSourceContext context)
    {
        var dataSetDefinitionProvider = context.ServiceProvider.GetRequiredService<IDataSetDefinitionProvider>();
        var linkedServiceDefinitionProvider = context.ServiceProvider.GetRequiredService<ILinkedServiceDefinitionProvider>();
        var dataSetDefinition = await dataSetDefinitionProvider.FindAsync(Name);
        var linkedServiceReference = dataSetDefinition!.LinkedServiceReference;
        var linkedServiceDefinition = await linkedServiceDefinitionProvider.FindAsync(linkedServiceReference.Name);
        var linkedService = linkedServiceDefinition!.LinkedService;
        var dataSet = dataSetDefinition.DataSet;
        var records = dataSet.ReadAsync<T>(linkedService);
        
        await foreach (var record in records)
            yield return record;
    }
    
}