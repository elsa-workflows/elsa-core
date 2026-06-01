using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.ModularPersistence.MongoDb.Extensions;
using Elsa.ModularPersistence.MongoDb.Options;

namespace Elsa.ModularPersistence.MongoDb.Features;

public sealed class MongoDbModularPersistenceFeature(IModule module) : FeatureBase(module)
{
    public Action<MongoDbModularPersistenceOptions>? ConfigureOptions { get; set; }

    public override void Apply()
    {
        Services.AddMongoDbModularPersistence(ConfigureOptions);
    }
}
