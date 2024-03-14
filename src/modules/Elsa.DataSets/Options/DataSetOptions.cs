using System.Text.Json.Serialization;
using Elsa.DataSets.Entities;

namespace Elsa.DataSets.Options;

/// <summary>
/// A configuration store for <see cref="DataSetDefinition"/>s.
/// </summary>
public class DataSetOptions
{
    /// <summary>
    /// Gets or sets the <see cref="DataSetDefinition"/>s.
    /// </summary>
    [JsonPropertyName("DataSets")]
    public ICollection<DataSetDefinition> DataSetDefinitions { get; set; } = new List<DataSetDefinition>();

    /// <summary>
    /// Gets or sets the <see cref="LinkedServiceDefinition"/>s.
    /// </summary>
    [JsonPropertyName("LinkedServices")]
    public ICollection<LinkedServiceDefinition> LinkedServiceDefinitions { get; set; } = new List<LinkedServiceDefinition>();
}