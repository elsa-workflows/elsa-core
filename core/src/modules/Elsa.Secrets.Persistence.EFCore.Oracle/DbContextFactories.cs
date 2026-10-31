using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Oracle.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EFCore.Oracle;

[UsedImplicitly]
public class SecretsDbContextFactory : OracleSecretsDesignTimeDbContextFactory<SecretsElsaDbContext>;

public class OracleSecretsDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString)
    {
        var options = new ElsaDbContextOptions().ConfigureSecretsOracle();
        builder.UseElsaOracle(GetType().Assembly, connectionString, options);
    }
}
