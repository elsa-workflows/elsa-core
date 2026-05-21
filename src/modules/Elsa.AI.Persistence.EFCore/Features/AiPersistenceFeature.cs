using Elsa.AI.Persistence.EFCore.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.AI.Persistence.EFCore.Features;

public class AiPersistenceFeature(IModule module) : FeatureBase(module)
{
    public override void Apply()
    {
        Services.AddAiPersistenceStores();
    }
}
