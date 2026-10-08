using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Persistence.EFCore.Sqlite;

[UsedImplicitly]
public class SqliteConnectionDbContextFactory : DesignTimeDbContextFactoryBase<ConnectionDbContext>
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<ConnectionDbContext> builder, string connectionString)
    {
        builder.UseElsaSqlite(GetType().Assembly, connectionString);
    }
}