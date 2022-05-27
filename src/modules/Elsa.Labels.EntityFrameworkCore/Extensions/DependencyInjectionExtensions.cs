using Elsa.Labels.EntityFrameworkCore.Features;
using Elsa.Labels.Features;

namespace Elsa.Labels.EntityFrameworkCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static LabelsFeature UseEntityFrameworkCore(this LabelsFeature feature, Action<EFCoreLabelPersistenceFeature> configure)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}