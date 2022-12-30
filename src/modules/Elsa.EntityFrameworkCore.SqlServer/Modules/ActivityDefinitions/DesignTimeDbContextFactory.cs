using Elsa.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.EntityFrameworkCore.SqlServer.Abstractions;

namespace Elsa.EntityFrameworkCore.SqlServer.Modules.ActivityDefinitions;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : SqlServerDesignTimeDbContextFactoryBase<ActivityDefinitionsElsaDbContext>
{
}