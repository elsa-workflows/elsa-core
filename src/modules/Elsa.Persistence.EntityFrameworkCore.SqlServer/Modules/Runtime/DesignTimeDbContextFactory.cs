using Elsa.Persistence.EntityFrameworkCore.Modules.Runtime;
using Elsa.Persistence.EntityFrameworkCore.SqlServer.Abstractions;

namespace Elsa.Persistence.EntityFrameworkCore.SqlServer.Modules.Runtime;

// ReSharper disable once UnusedType.Global
public class DesignTimeRuntimeDbContextFactory : SqlServerDesignTimeDbContextFactoryBase<RuntimeElsaDbContext>
{
}