using Elsa.EntityFrameworkCore.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Elsa.EntityFrameworkCore.Common;

/// <summary>
/// An optional base class to implement with some opinions on certain converters to install for certain DB providers.
/// </summary>
[PublicAPI]
public abstract class ElsaDbContextBase : DbContext
{
    /// <summary>
    /// The schema used by Elsa.
    /// </summary>
    public static string ElsaSchema { get; set;  } = "Elsa";

    /// <summary>
    /// The table used to store the migrations history.
    /// </summary>
    public static string MigrationsHistoryTable { get; set; } = "__EFMigrationsHistory";

    /// <summary>
    /// Initializes a new instance of the <see cref="ElsaDbContextBase"/> class.
    /// </summary>
    protected ElsaDbContextBase(DbContextOptions options) : base(options)
    {
        var elsaDbContextOptions = options.FindExtension<ElsaDbContextOptionsExtension>()?.Options;
        
        // ReSharper disable once VirtualMemberCallInConstructor
        Schema = !string.IsNullOrWhiteSpace(elsaDbContextOptions?.SchemaName) ? elsaDbContextOptions.SchemaName : ElsaSchema;
    }

    /// <summary>
    /// The schema used by Elsa.
    /// </summary>
    protected virtual string Schema { get; set; }

    /// <inheritdoc />
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

    /// <summary>
    /// Override this method to apply entity configurations.
    /// </summary>
    protected virtual void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
    }

    /// <summary>
    /// Override this method to apply entity configurations.
    /// </summary>
    protected virtual void Configure(ModelBuilder modelBuilder)
    {
    }

    /// <summary>
    /// Override this method to apply entity configurations for the SQLite provider.
    /// </summary>
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

    /// <summary>
    /// Override this method to apply entity configurations for the Oracle provider.
    /// </summary>
    protected virtual void SetupForOracle(ModelBuilder modelBuilder)
    {
    }
}