using Elsa.Labels.Features;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.Labels;

public static class Extensions
{
    public static LabelsFeature UseEntityFrameworkCore(this LabelsFeature feature, Action<EFCoreLabelPersistenceFeature> configure)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}