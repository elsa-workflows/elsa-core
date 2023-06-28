using Elsa.Labels.Features;

namespace Elsa.EntityFrameworkCore.Modules.Labels;

public static class Extensions
{
    public static LabelsFeature UseEntityFrameworkCore(this LabelsFeature feature, Action<EFCoreLabelPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}