using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Modules.Identity;
using JetBrains.Annotations;

namespace Elsa.EntityFrameworkCore.MySql.Modules.Identity;

/// <summary>
/// The design-time factory for the <see cref="IdentityElsaDbContext"/>.
/// </summary>
[PublicAPI]
public class DesignTimeIdentityDbContextFactory : MySqlDesignTimeDbContextFactoryBase<IdentityElsaDbContext>
{
}