using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.PostgreSql;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EntityFrameworkCore.PostgreSql;

[UsedImplicitly]
public class PostgreSqlSecretsDbContextFactory : PostgreSqlDesignTimeDbContextFactory<SecretsDbContext>
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<SecretsDbContext> builder, string connectionString)
    {
        builder.UseElsaPostgreSql(GetType().Assembly, connectionString);
    }
}