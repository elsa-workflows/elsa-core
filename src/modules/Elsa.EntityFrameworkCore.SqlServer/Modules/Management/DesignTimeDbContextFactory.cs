using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.SqlServer.Abstractions;

namespace Elsa.EntityFrameworkCore.SqlServer.Modules.Management;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : SqlServerDesignTimeDbContextFactoryBase<ManagementElsaDbContext>
{
}