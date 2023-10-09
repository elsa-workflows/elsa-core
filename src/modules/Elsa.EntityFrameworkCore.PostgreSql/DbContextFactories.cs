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

namespace Elsa.EntityFrameworkCore.PostgreSql;

[UsedImplicitly]
public class IdentityDbContextFactory : PostgreSqlDesignTimeDbContextFactory<IdentityElsaDbContext>
{
}

[UsedImplicitly]
public class ManagementDbContextFactory : PostgreSqlDesignTimeDbContextFactory<ManagementElsaDbContext>
{
}

[UsedImplicitly]
public class RuntimeDbContextFactory : PostgreSqlDesignTimeDbContextFactory<RuntimeElsaDbContext>
{
}

[UsedImplicitly]
public class LabelsDbContextFactory : PostgreSqlDesignTimeDbContextFactory<LabelsElsaDbContext>
{
}

[UsedImplicitly]
public class AlterationsDbContextFactories : PostgreSqlDesignTimeDbContextFactory<AlterationsElsaDbContext>
{
}

public class PostgreSqlDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString)
    {
        builder.UseElsaPostgreSql(GetType().Assembly, connectionString);
    }
}