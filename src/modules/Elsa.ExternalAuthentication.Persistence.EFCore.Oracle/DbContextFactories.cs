using Elsa.ExternalAuthentication.Persistence.EFCore.Oracle.Extensions;
using Elsa.ExternalAuthentication.Persistence.EFCore;
using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.Oracle;

[UsedImplicitly]
public class ExternalAuthenticationDbContextFactory : OracleExternalAuthenticationDesignTimeDbContextFactory<ExternalAuthenticationElsaDbContext>;

public class OracleExternalAuthenticationDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString)
    {
        var options = new ElsaDbContextOptions().ConfigureExternalAuthenticationOracle();
        builder.UseElsaOracle(GetType().Assembly, connectionString, options);
    }
}
