using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Modules.Runtime;

namespace Elsa.EntityFrameworkCore.SqlServer.Modules.Runtime;

// ReSharper disable once UnusedType.Global
/// <inheritdoc />
public class DesignTimeRuntimeDbContextFactory : SqlServerDesignTimeDbContextFactoryBase<RuntimeElsaDbContext>
{
}