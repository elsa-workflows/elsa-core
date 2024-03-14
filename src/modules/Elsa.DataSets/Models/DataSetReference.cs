using Elsa.DataSets.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.DataSets.Models;

/// <summary>
/// Represents a reference to a dataset.
/// </summary>
public class DataSetReference : IDataSource
{
    /// <summary>
    /// Gets or sets the name of the dataset reference.
    /// </summary>
    public string Name { get; set; }
    
    public static implicit operator DataSetReference(string name) => new() { Name = name };
    public static implicit operator string(DataSetReference reference) => reference.Name;
    
    public override string ToString() => Name;
    
    public async ValueTask<IEnumerable<object>> ListAsync(DataSourceContext context)
    {
        var store = context.ServiceProvider.GetRequiredService<IDataSetDefinitionStore>();
        var definition = await store.FindAsync(Name);
        //definition.DataSet.ReadAsync(context.ServiceProvider);
        return Array.Empty<object>();
    }

    public static bool operator ==(DataSetReference left, DataSetReference right) => left?.Name == right?.Name;
    public static bool operator !=(DataSetReference left, DataSetReference right) => left?.Name != right?.Name;
    public override bool Equals(object? obj) => obj is DataSetReference reference && reference.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
    
}