using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Modules.Management;

namespace Elsa.EntityFrameworkCore.PostgreSql.Modules.Management;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : PostgreSqlDesignTimeDbContextFactoryBase<ManagementElsaDbContext>
{
}