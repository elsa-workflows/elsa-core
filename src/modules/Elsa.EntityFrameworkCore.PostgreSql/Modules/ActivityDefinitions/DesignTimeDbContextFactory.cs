using Elsa.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.EntityFrameworkCore.PostgreSql.Abstractions;

namespace Elsa.EntityFrameworkCore.PostgreSql.Modules.ActivityDefinitions;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : PostgreSqlDesignTimeDbContextFactoryBase<ActivityDefinitionsElsaDbContext>
{
}