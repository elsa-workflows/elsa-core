using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Modules.Management;

namespace Elsa.EntityFrameworkCore.MySql.Modules.Management;

// ReSharper disable once UnusedType.Global
/// <inheritdoc />
public class DesignTimeDbContextFactory : MySqlDesignTimeDbContextFactoryBase<ManagementElsaDbContext>
{
}