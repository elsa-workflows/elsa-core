using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Elsa.EntityFrameworkCore.MySql;

[UsedImplicitly]
public class IdentityDbContextFactory : MySqlDesignTimeDbContextFactory<IdentityElsaDbContext>
{
}

[UsedImplicitly]
public class ManagementDbContextFactory : MySqlDesignTimeDbContextFactory<ManagementElsaDbContext>
{
}

[UsedImplicitly]
public class RuntimeDbContextFactory : MySqlDesignTimeDbContextFactory<RuntimeElsaDbContext>
{
}

[UsedImplicitly]
public class LabelsDbContextFactory : MySqlDesignTimeDbContextFactory<LabelsElsaDbContext>
{
}

[UsedImplicitly]
public class AlterationsDbContextFactories : MySqlDesignTimeDbContextFactory<AlterationsElsaDbContext>
{
}

public class MySqlDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString)
    {
        builder.UseElsaMySql(GetType().Assembly, connectionString);
    }
}