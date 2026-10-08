using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EFCore.SqlServer;

[UsedImplicitly]
public class SecretsDbContextFactory : SqlServerSecretsDesignTimeDbContextFactory<SecretsElsaDbContext>;

public class SqlServerSecretsDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString)
    {
        builder.UseElsaSqlServer(GetType().Assembly, connectionString);
    }
}
