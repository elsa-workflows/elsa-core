using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.EntityFrameworkCore.MySql.Abstractions;

namespace Elsa.EntityFrameworkCore.MySql.Modules.Runtime;

// ReSharper disable once UnusedType.Global
/// <inheritdoc />
public class DesignTimeRuntimeDbContextFactory : MySqlDesignTimeDbContextFactoryBase<RuntimeElsaDbContext>
{
}