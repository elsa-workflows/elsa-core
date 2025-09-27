using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using Elsa.Persistence.EFCore.Modules.Identity;
using Elsa.Persistence.EFCore.Modules.Labels;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Persistence.EFCore.Modules.Tenants;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Elsa.Persistence.EFCore.Oracle;

[UsedImplicitly]
public class IdentityDbContextFactory : OracleDesignTimeDbContextFactory<IdentityElsaDbContext>;

[UsedImplicitly]
public class ManagementDbContextFactory : OracleDesignTimeDbContextFactory<ManagementElsaDbContext>;

[UsedImplicitly]
public class RuntimeDbContextFactory : OracleDesignTimeDbContextFactory<RuntimeElsaDbContext>;

[UsedImplicitly]
public class LabelsDbContextFactory : OracleDesignTimeDbContextFactory<LabelsElsaDbContext>;

[UsedImplicitly]
public class AlterationsDbContextFactories : OracleDesignTimeDbContextFactory<AlterationsElsaDbContext>;

[UsedImplicitly]
public class TenantsDbContextFactories : OracleDesignTimeDbContextFactory<TenantsElsaDbContext>;

public class OracleDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString)
    {
        builder.UseElsaOracle(GetType().Assembly, connectionString);
    }
}