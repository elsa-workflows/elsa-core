using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql;

[UsedImplicitly]
public sealed class ConnectionsDbContextFactory : PostgreSqlConnectionsDesignTimeDbContextFactory<ConnectionsElsaDbContext>;

public class PostgreSqlConnectionsDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext>
    where TDbContext : DbContext
{
    protected override void ConfigureServices(IServiceCollection services) => services.AddPostgreSqlEntityModelCreatingHandlers();

    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString) =>
        builder.UseElsaPostgreSql(GetType().Assembly, connectionString);
}
