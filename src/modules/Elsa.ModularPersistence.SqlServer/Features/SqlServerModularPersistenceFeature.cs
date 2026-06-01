using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.ModularPersistence.SqlServer.Extensions;
using Elsa.ModularPersistence.SqlServer.Options;

namespace Elsa.ModularPersistence.SqlServer.Features;

public sealed class SqlServerModularPersistenceFeature(IModule module) : FeatureBase(module)
{
    public Action<SqlServerModularPersistenceOptions>? ConfigureOptions { get; set; }

    public override void Apply()
    {
        Services.AddSqlServerModularPersistence(ConfigureOptions);
    }
}
