using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Sqlite.Abstractions;

namespace Elsa.EntityFrameworkCore.Sqlite.Modules.Management;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : SqliteDesignTimeDbContextFactoryBase<ManagementElsaDbContext>
{
}