using Elsa.DataSets.Entities;

namespace Elsa.DataSets.Options;

public class DataSetDefinitionOptions
{
    public ICollection<DataSetDefinition> DataSetDefinitions { get; set; } = new List<DataSetDefinition>();
}