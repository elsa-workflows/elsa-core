using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Persistence.EFCore.MySql;

[UsedImplicitly]
public class SecretsDbContextFactory : MySqlSecretsDesignTimeDbContextFactory<SecretsElsaDbContext>;

public class MySqlSecretsDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
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
