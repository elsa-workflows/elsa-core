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

namespace Elsa.EntityFrameworkCore.SqlServer;

[UsedImplicitly]
public class IdentityDbContextFactory : SqlServerDesignTimeDbContextFactory<IdentityElsaDbContext>
{
}

[UsedImplicitly]
public class ManagementDbContextFactory : SqlServerDesignTimeDbContextFactory<ManagementElsaDbContext>
{
}

[UsedImplicitly]
public class RuntimeDbContextFactory : SqlServerDesignTimeDbContextFactory<RuntimeElsaDbContext>
{
}

[UsedImplicitly]
public class LabelsDbContextFactory : SqlServerDesignTimeDbContextFactory<LabelsElsaDbContext>
{
}

[UsedImplicitly]
public class AlterationsDbContextFactories : SqlServerDesignTimeDbContextFactory<AlterationsElsaDbContext>
{
}

public class SqlServerDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString)
    {
        builder.UseElsaSqlServer(GetType().Assembly, connectionString);
    }
}