using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.Persistence.VNext.Extensions.Features;

public class PersistenceVNextFeature(IModule module) : FeatureBase(module)
{
    public Action<PersistenceVNextOptions>? ConfigureOptions { get; set; }

    public override void Apply()
    {
        Services.AddPersistenceVNext(ConfigureOptions);
    }
}
