using Elsa.ExternalAuthentication.Persistence.EFCore;
using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.SqlServer;

[UsedImplicitly]
public class ExternalAuthenticationDbContextFactory : SqlServerExternalAuthenticationDesignTimeDbContextFactory<ExternalAuthenticationElsaDbContext>;

public class SqlServerExternalAuthenticationDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString)
    {
        builder.UseElsaSqlServer(GetType().Assembly, connectionString);
    }
}
