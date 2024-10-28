using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.MySql;
using Elsa.EntityFrameworkCore.Abstractions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Quartz.EntityFrameworkCore.MySql;

/// <summary>
/// Quartz DbContext for MySQL.
/// </summary>
[UsedImplicitly]
public class MySqlQuartzDbContext : DbContext
{
    /// <inheritdoc />
    public MySqlQuartzDbContext(DbContextOptions<MySqlQuartzDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddQuartz(model => model.UseMySql());
}

/// <summary>
/// Design-time factory for Quartz DbContext for MySQL.
/// </summary>
[UsedImplicitly]
public class MySqlQuartzDbContextFactory : DesignTimeDbContextFactoryBase<MySqlQuartzDbContext>
{
    /// <inheritdoc />
    protected override void ConfigureBuilder(DbContextOptionsBuilder<MySqlQuartzDbContext> builder, string connectionString) => builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
}