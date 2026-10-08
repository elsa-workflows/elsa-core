using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Persistence.EFCore.Sqlite;

[UsedImplicitly]
public class AIDbContextFactory : SqliteAIDesignTimeDbContextFactory<AIDbContext>;

public class SqliteAIDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSqliteEntityModelCreatingHandlers();
    }

    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString)
    {
        builder.UseElsaSqlite(GetType().Assembly, connectionString);
    }
}
