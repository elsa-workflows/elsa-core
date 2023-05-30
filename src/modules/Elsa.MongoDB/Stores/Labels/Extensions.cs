using Elsa.Labels.Features;

namespace Elsa.MongoDB.Stores.Labels;

public static class Extensions
{
    public static LabelsFeature UseMongoDb(this LabelsFeature feature, Action<MongoLabelPersistenceFeature> configure)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}