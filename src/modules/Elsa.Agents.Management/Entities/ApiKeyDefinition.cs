using Elsa.Common.Entities;

namespace Elsa.Agents.Management;

public class ApiKeyDefinition : Entity
{
    public string Name { get; set; } = default!;
    public string Value { get; set; } = default!;
}