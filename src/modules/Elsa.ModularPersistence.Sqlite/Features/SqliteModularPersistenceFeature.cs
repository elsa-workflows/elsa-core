using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.ModularPersistence.Sqlite.Extensions;
using Elsa.ModularPersistence.Sqlite.Options;

namespace Elsa.ModularPersistence.Sqlite.Features;

public sealed class SqliteModularPersistenceFeature(IModule module) : FeatureBase(module)
{
    public Action<SqliteModularPersistenceOptions>? ConfigureOptions { get; set; }

    public override void Apply()
    {
        Services.AddSqliteModularPersistence(ConfigureOptions);
    }
}
