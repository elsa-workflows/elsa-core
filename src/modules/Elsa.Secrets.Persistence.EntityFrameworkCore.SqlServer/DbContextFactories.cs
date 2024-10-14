using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.SqlServer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EntityFrameworkCore.SqlServer;

[UsedImplicitly]
public class SqlServerSecretsDbContextFactory : SqlServerDesignTimeDbContextFactory<SecretsDbContext>
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<SecretsDbContext> builder, string connectionString)
    {
        builder.UseElsaSqlServer(GetType().Assembly, connectionString);
    }
}