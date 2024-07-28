using JetBrains.Annotations;

namespace Elsa.SemanticKernel;

[UsedImplicitly]
public class ApiKeyConfig
{
    public string Name { get; set; }
    public string Value { get; set; }
}