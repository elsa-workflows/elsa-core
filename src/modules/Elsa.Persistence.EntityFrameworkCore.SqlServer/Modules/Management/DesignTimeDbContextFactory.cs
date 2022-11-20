using Elsa.Persistence.EntityFrameworkCore.Modules.Management;
using Elsa.Persistence.EntityFrameworkCore.SqlServer.Abstractions;

namespace Elsa.Persistence.EntityFrameworkCore.SqlServer.Modules.Management;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : SqlServerDesignTimeDbContextFactoryBase<ManagementElsaDbContext>
{
}