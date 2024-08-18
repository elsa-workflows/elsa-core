using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Agents.Persistence.EntityFrameworkCore.Sqlite;

[UsedImplicitly]
public class SqliteAgentsDbContextFactory : DesignTimeDbContextFactoryBase<AgentsDbContext>
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<AgentsDbContext> builder, string connectionString)
    {
        builder.UseElsaSqlite(GetType().Assembly, connectionString);
    }
}