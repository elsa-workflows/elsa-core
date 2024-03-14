using Elsa.Common.Entities;
using Elsa.DataSets.Contracts;

namespace Elsa.DataSets.Entities;

public class LinkedServiceDefinition : Entity
{
    public string Name { get; set; } = default!;
    public ILinkedService LinkedService { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? Description { get; set; }
}