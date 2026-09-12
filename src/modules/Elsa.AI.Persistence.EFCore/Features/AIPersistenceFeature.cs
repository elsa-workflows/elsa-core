using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Persistence.EFCore.Features;

public class AIPersistenceFeature(IModule module) : PersistenceFeatureBase<AIPersistenceFeature, AIDbContext>(module)
{
    public Action<DbContextOptionsBuilder>? ConfigureDbContext { get; set; }

    public override void Apply()
    {
        if (DbContextOptionsBuilder == null && ConfigureDbContext != null)
            DbContextOptionsBuilder = (_, builder) => ConfigureDbContext(builder);

        base.Apply();
        Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AIDbContext>>().CreateDbContext());
        Services.AddAIPersistenceStoreServices();
    }
}
