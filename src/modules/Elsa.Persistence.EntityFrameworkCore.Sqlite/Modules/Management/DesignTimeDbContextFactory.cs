using Elsa.Persistence.EntityFrameworkCore.Modules.Management;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Abstractions;

namespace Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.Management;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : SqliteDesignTimeDbContextFactoryBase<ManagementDbContext>
{
}