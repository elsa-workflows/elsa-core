using JetBrains.Annotations;

namespace Elsa.Agents;

[UsedImplicitly]
public class ApiKeyConfig
{
    public string Name { get; set; }
    public string Value { get; set; }
}