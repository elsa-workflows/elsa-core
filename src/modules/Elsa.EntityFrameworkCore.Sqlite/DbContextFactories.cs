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

namespace Elsa.EntityFrameworkCore.Sqlite;

[UsedImplicitly]
public class IdentityDbContextFactory : SqliteDesignTimeDbContextFactory<IdentityElsaDbContext>
{
}

[UsedImplicitly]
public class ManagementDbContextFactory : SqliteDesignTimeDbContextFactory<ManagementElsaDbContext>
{
}

[UsedImplicitly]
public class RuntimeDbContextFactory : SqliteDesignTimeDbContextFactory<RuntimeElsaDbContext>
{
}

[UsedImplicitly]
public class LabelsDbContextFactory : SqliteDesignTimeDbContextFactory<LabelsElsaDbContext>
{
}

[UsedImplicitly]
public class AlterationsDbContextFactories : SqliteDesignTimeDbContextFactory<AlterationsElsaDbContext>
{
}

public class SqliteDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString)
    {
        builder.UseElsaSqlite(GetType().Assembly, connectionString);
    }
}