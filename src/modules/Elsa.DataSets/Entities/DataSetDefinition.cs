using System.Text.Json.Serialization;
using Elsa.Common.Entities;
using Elsa.DataSets.Contracts;

namespace Elsa.DataSets.Entities;

public class DataSetDefinition : Entity
{
    public string Name { get; set; } = default!;
    public IDataSet DataSet { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? Description { get; set; }
}