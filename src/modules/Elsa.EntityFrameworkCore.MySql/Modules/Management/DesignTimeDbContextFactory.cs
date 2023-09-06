using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.MySql.Abstractions;

namespace Elsa.EntityFrameworkCore.MySql.Modules.Management;

// ReSharper disable once UnusedType.Global
/// <inheritdoc />
public class DesignTimeDbContextFactory : MySqlDesignTimeDbContextFactoryBase<ManagementElsaDbContext>
{
}