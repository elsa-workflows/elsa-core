using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using Elsa.Persistence.EFCore.Modules.Identity;
using Elsa.Persistence.EFCore.Modules.Labels;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Persistence.EFCore.Modules.Tenants;
using Elsa.Persistence.EFCore.MySql.Handlers;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Elsa.Persistence.EFCore.MySql;

[UsedImplicitly]
public class IdentityDbContextFactory : MySqlDesignTimeDbContextFactory<IdentityElsaDbContext>;

[UsedImplicitly]
public class ManagementDbContextFactory : MySqlDesignTimeDbContextFactory<ManagementElsaDbContext>;

[UsedImplicitly]
public class RuntimeDbContextFactory : MySqlDesignTimeDbContextFactory<RuntimeElsaDbContext>;

[UsedImplicitly]
public class LabelsDbContextFactory : MySqlDesignTimeDbContextFactory<LabelsElsaDbContext>;

[UsedImplicitly]
public class AlterationsDbContextFactories : MySqlDesignTimeDbContextFactory<AlterationsElsaDbContext>;

[UsedImplicitly]
public class TenantsDbContextFactories : MySqlDesignTimeDbContextFactory<TenantsElsaDbContext>;

public class MySqlDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IEntityModelCreatingHandler, SetupForMySql>();
    }

    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString)
    {
        builder.UseElsaMySql(GetType().Assembly, connectionString, serverVersion: ServerVersion.Parse("9.0.0"));
    }
}