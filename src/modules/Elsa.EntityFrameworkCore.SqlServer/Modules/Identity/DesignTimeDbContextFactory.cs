using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.SqlServer.Abstractions;
using JetBrains.Annotations;

namespace Elsa.EntityFrameworkCore.SqlServer.Modules.Identity;

/// <summary>
/// The design-time factory for the <see cref="IdentityElsaDbContext"/>.
/// </summary>
[PublicAPI]
public class DesignTimeIdentityDbContextFactory : SqlServerDesignTimeDbContextFactoryBase<IdentityElsaDbContext>
{
}