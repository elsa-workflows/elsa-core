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
        var store = context.ServiceProvider.GetRequiredService<IDataSetDefinitionStore>();
        var definition = await store.FindAsync(Name);
        //definition.DataSet.ReadAsync(context.ServiceProvider);
        yield break;
    }
    
}