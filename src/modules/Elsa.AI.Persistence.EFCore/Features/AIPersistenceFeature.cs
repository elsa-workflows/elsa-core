using Elsa.AI.Persistence.EFCore.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.EntityFrameworkCore;

namespace Elsa.AI.Persistence.EFCore.Features;

public class AIPersistenceFeature(IModule module) : FeatureBase(module)
{
    public Action<DbContextOptionsBuilder>? ConfigureDbContext { get; set; }

    public override void Apply()
    {
        Services.AddAIPersistenceStores(ConfigureDbContext);
    }
}
