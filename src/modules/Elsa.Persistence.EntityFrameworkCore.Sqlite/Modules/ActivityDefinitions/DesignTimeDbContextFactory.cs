using Elsa.Persistence.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Abstractions;

namespace Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.ActivityDefinitions;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : SqliteDesignTimeDbContextFactoryBase<ActivityDefinitionsDbContext>
{
}