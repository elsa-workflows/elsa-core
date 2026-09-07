using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// Generates a migration script offline, through <see cref="IMigrator.GenerateScript"/>, so tests can assert on the
/// SQL a migration produces without opening a database connection.
/// </summary>
internal static class MigrationScriptGenerator
{
    /// <summary>
    /// Builds a <typeparamref name="TDbContext"/> configured by <paramref name="configure"/> and generates the
    /// migration script between <paramref name="fromMigration"/> and <paramref name="toMigration"/>.
    /// </summary>
    /// <param name="configure">Configures the provider (and, through it, the migrations assembly and schema) on the options builder.</param>
    /// <param name="fromMigration">The migration to generate the script from, or <c>null</c> for the initial database state.</param>
    /// <param name="toMigration">The migration to generate the script up to.</param>
    /// <param name="options">The SQL generation options, for example whether the script is idempotent.</param>
    public static string Generate<TDbContext>(
        Action<DbContextOptionsBuilder<TDbContext>> configure,
        string? fromMigration,
        string toMigration,
        MigrationsSqlGenerationOptions options)
        where TDbContext : DbContext
    {
        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
        configure(optionsBuilder);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        using var dbContext = (TDbContext)Activator.CreateInstance(typeof(TDbContext), optionsBuilder.Options, serviceProvider)!;

        return dbContext.GetService<IMigrator>().GenerateScript(fromMigration: fromMigration, toMigration: toMigration, options: options);
    }
}
