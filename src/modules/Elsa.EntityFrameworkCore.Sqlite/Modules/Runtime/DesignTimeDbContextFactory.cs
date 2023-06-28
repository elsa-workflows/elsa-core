using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.EntityFrameworkCore.Sqlite.Abstractions;

namespace Elsa.EntityFrameworkCore.Sqlite.Modules.Runtime;

// ReSharper disable once UnusedType.Global
/// <inheritdoc />
public class DesignTimeRuntimeDbContextFactory : SqliteDesignTimeDbContextFactoryBase<RuntimeElsaDbContext>
{
}