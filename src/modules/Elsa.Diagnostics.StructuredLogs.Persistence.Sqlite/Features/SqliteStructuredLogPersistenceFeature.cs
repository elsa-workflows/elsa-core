using Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Extensions;
using Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Options;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Features;

public class SqliteStructuredLogPersistenceFeature(IModule module) : FeatureBase(module)
{
    public Action<SqliteStructuredLogOptions>? ConfigureOptions { get; set; }

    public override void Apply()
    {
        Services.AddSqliteStructuredLogPersistence(ConfigureOptions);
    }
}
