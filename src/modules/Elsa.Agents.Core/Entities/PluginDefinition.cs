using Elsa.Common.Entities;

namespace Elsa.Agents;

public class PluginDefinition : Entity
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
}