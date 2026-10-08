using Elsa.Labels.Features;

namespace Elsa.Persistence.MongoDb.Modules.Labels;

public static class Extensions
{
    public static LabelsFeature UseMongoDb(this LabelsFeature feature, Action<MongoLabelPersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}