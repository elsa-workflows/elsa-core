using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.AI.Persistence.EFCore.MySql;

[UsedImplicitly]
public class AIDbContextFactory : MySqlAIDesignTimeDbContextFactory<AIDbContext>;

public class MySqlAIDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddMySqlEntityModelCreatingHandlers();
    }

    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString)
    {
        builder.UseElsaMySql(GetType().Assembly, connectionString, serverVersion: ServerVersion.Parse("9.0.0"));
    }
}
