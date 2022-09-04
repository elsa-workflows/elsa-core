using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Elsa.Persistence.EntityFrameworkCore.Common;

/// <summary>
/// An optional base class to implement with some opinions on certain converters to install for certain DB providers.
/// </summary>
public abstract class DbContextBase : DbContext
{
    public const string ElsaSchema = "Elsa";
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    protected DbContextBase(DbContextOptions options) : base(options)
    {
    }

    protected virtual string Schema => ElsaSchema;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (!string.IsNullOrWhiteSpace(Schema))
        {
            if (!Database.IsSqlite())
                modelBuilder.HasDefaultSchema(Schema);
        }
        
        ApplyEntityConfigurations(modelBuilder);

        if(Database.IsSqlite()) SetupForSqlite(modelBuilder);
        if(Database.IsOracle()) SetupForOracle(modelBuilder);
    }

    protected virtual void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
    }

    protected virtual void Configure(ModelBuilder modelBuilder)
    {
    }

    protected virtual void SetupForSqlite(ModelBuilder modelBuilder)
    {
        // SQLite does not have proper support for DateTimeOffset via Entity Framework Core, see the limitations
        // here: https://docs.microsoft.com/en-us/ef/core/providers/sqlite/limitations#query-limitations
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?));

            foreach (var property in properties)
            {
                modelBuilder
                    .Entity(entityType.Name)
                    .Property(property.Name)
                    .HasConversion(new DateTimeOffsetToStringConverter());
            }
        }
    }

    protected virtual void SetupForOracle(ModelBuilder modelBuilder)
    {
    }
}