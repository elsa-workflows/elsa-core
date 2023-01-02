using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.EntityFrameworkCore.PostgreSql.Abstractions;

namespace Elsa.EntityFrameworkCore.PostgreSql.Modules.Runtime;

// ReSharper disable once UnusedType.Global
public class DesignTimeRuntimeDbContextFactory : PostgreSqlDesignTimeDbContextFactoryBase<RuntimeElsaDbContext>
{
}