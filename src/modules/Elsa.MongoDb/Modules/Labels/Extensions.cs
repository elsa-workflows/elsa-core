using Elsa.Labels.Features;

namespace Elsa.MongoDb.Modules.Labels;

public static class Extensions
{
    public static LabelsFeature UseMongoDb(this LabelsFeature feature, Action<MongoLabelPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}