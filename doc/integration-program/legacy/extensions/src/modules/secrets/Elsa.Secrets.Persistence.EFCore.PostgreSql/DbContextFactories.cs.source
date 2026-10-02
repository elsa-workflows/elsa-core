using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.PostgreSql;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EFCore.PostgreSql;

[UsedImplicitly]
public class PostgreSqlSecretsDbContextFactory : PostgreSqlDesignTimeDbContextFactory<SecretsDbContext>
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<SecretsDbContext> builder, string connectionString)
    {
        builder.UseElsaPostgreSql(GetType().Assembly, connectionString);
    }
}