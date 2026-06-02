using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.ModularPersistence.PostgreSql.Extensions;
using Elsa.ModularPersistence.PostgreSql.Options;

namespace Elsa.ModularPersistence.PostgreSql.Features;

public sealed class PostgreSqlModularPersistenceFeature(IModule module) : FeatureBase(module)
{
    public Action<PostgreSqlModularPersistenceOptions>? ConfigureOptions { get; set; }

    public override void Apply()
    {
        Services.AddPostgreSqlModularPersistence(ConfigureOptions);
    }
}
