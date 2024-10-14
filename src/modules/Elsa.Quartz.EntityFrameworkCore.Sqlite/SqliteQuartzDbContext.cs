using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.SQLite;
using Elsa.EntityFrameworkCore.Abstractions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Quartz.EntityFrameworkCore.Sqlite;

/// <summary>
/// Quartz DbContext for SQLite.
/// </summary>
[UsedImplicitly]
public class SqliteQuartzDbContext : DbContext
{
    /// <inheritdoc />
    public SqliteQuartzDbContext(DbContextOptions<SqliteQuartzDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddQuartz(model => model.UseSqlite());
}

/// <summary>
/// Design-time factory for Quartz DbContext for SQLite.
/// </summary>
[UsedImplicitly]
public class SqliteQuartzDbContextFactory : DesignTimeDbContextFactoryBase<SqliteQuartzDbContext>
{
    /// <inheritdoc />
    protected override void ConfigureBuilder(DbContextOptionsBuilder<SqliteQuartzDbContext> builder, string connectionString) => builder.UseSqlite(connectionString);
}