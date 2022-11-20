using Elsa.Persistence.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.Persistence.EntityFrameworkCore.SqlServer.Abstractions;

namespace Elsa.Persistence.EntityFrameworkCore.SqlServer.Modules.ActivityDefinitions;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : SqlServerDesignTimeDbContextFactoryBase<ActivityDefinitionsElsaDbContext>
{
}