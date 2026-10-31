using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.SqlServer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EFCore.SqlServer;

[UsedImplicitly]
public class SqlServerSecretsDbContextFactory : SqlServerDesignTimeDbContextFactory<SecretsDbContext>
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<SecretsDbContext> builder, string connectionString)
    {
        builder.UseElsaSqlServer(GetType().Assembly, connectionString);
    }
}