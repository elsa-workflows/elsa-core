using Elsa.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.EntityFrameworkCore.Sqlite.Abstractions;

namespace Elsa.EntityFrameworkCore.Sqlite.Modules.ActivityDefinitions;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : SqliteDesignTimeDbContextFactoryBase<ActivityDefinitionsElsaDbContext>
{
}