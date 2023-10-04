using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using JetBrains.Annotations;

namespace Elsa.EntityFrameworkCore.Sqlite.Modules.Runtime;


/// <summary>
/// The design-time factory for the <see cref="RuntimeElsaDbContext"/>.
/// </summary>
[PublicAPI]
public class DesignTimeRuntimeDbContextFactory : SqliteDesignTimeDbContextFactoryBase<RuntimeElsaDbContext>
{
}