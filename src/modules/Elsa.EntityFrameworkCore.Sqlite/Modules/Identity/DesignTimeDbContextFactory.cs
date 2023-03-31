using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Sqlite.Abstractions;
using JetBrains.Annotations;

namespace Elsa.EntityFrameworkCore.Sqlite.Modules.Identity;

/// <summary>
/// The design-time factory for the <see cref="IdentityElsaDbContext"/>.
/// </summary>
[PublicAPI]
public class DesignTimeIdentityDbContextFactory : SqliteDesignTimeDbContextFactoryBase<IdentityElsaDbContext>
{
}