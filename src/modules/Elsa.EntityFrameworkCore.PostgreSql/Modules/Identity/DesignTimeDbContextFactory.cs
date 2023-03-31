using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.PostgreSql.Abstractions;
using JetBrains.Annotations;

namespace Elsa.EntityFrameworkCore.PostgreSql.Modules.Identity;

/// <summary>
/// The design-time factory for the <see cref="IdentityElsaDbContext"/>.
/// </summary>
[PublicAPI]
public class DesignTimeIdentityDbContextFactory : PostgreSqlDesignTimeDbContextFactoryBase<IdentityElsaDbContext>
{
}