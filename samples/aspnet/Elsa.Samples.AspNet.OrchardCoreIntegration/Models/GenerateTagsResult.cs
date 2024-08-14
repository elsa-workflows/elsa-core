using JetBrains.Annotations;

namespace Elsa.Samples.AspNet.OrchardCoreIntegration;

[UsedImplicitly]
public class GenerateTagsResult
{
    public ICollection<string> Tags { get; set; } = new List<string>();
}