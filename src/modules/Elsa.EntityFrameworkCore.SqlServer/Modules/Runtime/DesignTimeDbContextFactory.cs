using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.EntityFrameworkCore.SqlServer.Abstractions;

namespace Elsa.EntityFrameworkCore.SqlServer.Modules.Runtime;

// ReSharper disable once UnusedType.Global
/// <inheritdoc />
public class DesignTimeRuntimeDbContextFactory : SqlServerDesignTimeDbContextFactoryBase<RuntimeElsaDbContext>
{
}