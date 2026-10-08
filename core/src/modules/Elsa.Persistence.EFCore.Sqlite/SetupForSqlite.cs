using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Elsa.Persistence.EFCore.Sqlite;

/// <summary>
/// Represents a class that handles entity model creation for SQLite databases.
/// </summary>
public class SetupForSqlite : IEntityModelCreatingHandler
{
    /// <inheritdoc />
    public void Handle(ElsaDbContextBase dbContext, ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if(!dbContext.Database.IsSqlite())
            return;
        
        // SQLite does not have proper support for DateTimeOffset via Entity Framework Core, see the limitations
        // here: https://docs.microsoft.com/en-us/ef/core/providers/sqlite/limitations#query-limitations
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