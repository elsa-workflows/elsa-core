using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Modules.Runtime;

namespace Elsa.EntityFrameworkCore.MySql.Modules.Runtime;

// ReSharper disable once UnusedType.Global
/// <inheritdoc />
public class DesignTimeRuntimeDbContextFactory : MySqlDesignTimeDbContextFactoryBase<RuntimeElsaDbContext>
{
}