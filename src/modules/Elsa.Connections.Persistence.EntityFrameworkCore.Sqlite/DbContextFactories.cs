using Elsa.EntityFrameworkCore.Abstractions;
using Elsa.EntityFrameworkCore.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Persistence.EntityFrameworkCore.Sqlite;

[UsedImplicitly]
public class SqliteConnectionDbContextFactory : DesignTimeDbContextFactoryBase<ConnectionDbContext>
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<ConnectionDbContext> builder, string connectionString)
    {
        builder.UseElsaSqlite(GetType().Assembly, connectionString);
    }
}