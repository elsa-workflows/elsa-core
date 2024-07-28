using JetBrains.Annotations;

namespace Elsa.Samples.AspNet.OrchardCoreIntegration;

[UsedImplicitly]
public class TranslationResult
{
    public string Translation { get; set; }
    public string CultureCode { get; set; }
}