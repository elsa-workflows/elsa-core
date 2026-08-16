using Elsa.ExternalAuthentication.Persistence.EFCore;
using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.MySql;

[UsedImplicitly]
public class ExternalAuthenticationDbContextFactory : MySqlExternalAuthenticationDesignTimeDbContextFactory<ExternalAuthenticationElsaDbContext>;

public class MySqlExternalAuthenticationDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
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
