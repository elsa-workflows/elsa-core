using Elsa.Labels.Features;

namespace Elsa.Persistence.EFCore.Modules.Labels;

public static class Extensions
{
    public static LabelsFeature UseEntityFrameworkCore(this LabelsFeature feature, Action<EFCoreLabelPersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}