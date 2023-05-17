using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.MySql.Abstractions;
using JetBrains.Annotations;

namespace Elsa.EntityFrameworkCore.MySql.Modules.Identity;

/// <summary>
/// The design-time factory for the <see cref="IdentityElsaDbContext"/>.
/// </summary>
[PublicAPI]
public class DesignTimeIdentityDbContextFactory : MySqlDesignTimeDbContextFactoryBase<IdentityElsaDbContext>
{
}