using JetBrains.Annotations;

namespace Elsa.Samples.AspNet.OrchardCoreIntegration;

[UsedImplicitly]
public class FactCheckResult
{
    public ICollection<IncorrectFact> IncorrectFacts { get; set; } = new List<IncorrectFact>();
}