using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.PostgreSql.Abstractions;

namespace Elsa.EntityFrameworkCore.PostgreSql.Modules.Management;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : PostgreSqlDesignTimeDbContextFactoryBase<ManagementElsaDbContext>
{
}