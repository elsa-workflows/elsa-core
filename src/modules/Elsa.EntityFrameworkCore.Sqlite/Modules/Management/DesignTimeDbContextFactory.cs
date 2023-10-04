using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Modules.Management;
using JetBrains.Annotations;

namespace Elsa.EntityFrameworkCore.Sqlite.Modules.Management;

/// <summary>
/// The design-time factory for the <see cref="ManagementElsaDbContext"/>.
/// </summary>
[PublicAPI]
public class DesignTimeDbContextFactory : SqliteDesignTimeDbContextFactoryBase<ManagementElsaDbContext>
{
}