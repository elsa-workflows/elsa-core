using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.Secrets.Persistence.EntityFrameworkCore;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Agents.Persistence.EntityFrameworkCore.Sqlite;

[UsedImplicitly]
public class SqliteSecretsDbContextFactory : DesignTimeDbContextFactoryBase<SecretsDbContext>
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<SecretsDbContext> builder, string connectionString)
    {
        builder.UseElsaSqlite(GetType().Assembly, connectionString);
    }
}