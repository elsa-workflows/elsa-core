using Elsa.Common.Entities;
using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Common;
/// <summary>
/// An optional base class to implement with some opinions on certain converters to install for certain DB providers.
/// </summary>
public abstract class ElsaDbContextBase : DbContext, IElsaDbContextSchema
{
    /// <summary>
    /// The default schema used by Elsa.
    /// </summary>
    public static string ElsaSchema { get; set;  } = "Elsa";
    private string _schema;
    /// <inheritdoc/>
    public string Schema => _schema;
    /// <summary>
    /// The table used to store the migrations history.
    /// </summary>
    public static string MigrationsHistoryTable { get; set; } = "__EFMigrationsHistory";

    /// <summary>
    /// Service Provider used in some strategies and filters.
    /// </summary>
    protected readonly IServiceProvider _serviceProvider;

    private readonly IEnumerable<IDbContextStrategy> _dbContextStrategies;
    private readonly Action<ModelBuilder, IServiceProvider>? _additionnalEntityConfigurations;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElsaDbContextBase"/> class.
    /// </summary>
    protected ElsaDbContextBase(DbContextOptions options, IServiceProvider serviceProvider) : base(options)
    {
        var elsaDbContextOptions = options.FindExtension<ElsaDbContextOptionsExtension>()?.Options;

        // ReSharper disable once VirtualMemberCallInConstructor
        _schema = !string.IsNullOrWhiteSpace(elsaDbContextOptions?.SchemaName) ? elsaDbContextOptions.SchemaName : ElsaSchema;

        _serviceProvider = serviceProvider;
        _dbContextStrategies = serviceProvider.GetServices<IDbContextStrategy>();
    }

    /// <summary>
    /// The schema used by Elsa.
    /// </summary>
   // protected virtual string Schema { get; set; }

    /// <inheritdoc/>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await OnBeforeSaving();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (!string.IsNullOrWhiteSpace(Schema))
        {
            if (!Database.IsSqlite())
                modelBuilder.HasDefaultSchema(Schema);
        }

        ApplyEntityConfigurations(modelBuilder);

        if (Database.IsSqlite()) SetupForSqlite(modelBuilder);
        if (Database.IsOracle()) SetupForOracle(modelBuilder);


        _additionnalEntityConfigurations?.Invoke(modelBuilder, _serviceProvider);
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

    private async Task OnBeforeSaving()
    {
        foreach (EntityEntry entry in ChangeTracker.Entries().Where(IsModifiedEntity))
        {
            IEnumerable<IBeforeSavingDbContextStrategy> beforeSavingDbContextStrategies = _dbContextStrategies
                .OfType<IBeforeSavingDbContextStrategy>()
                .Where(strategy => strategy.CanExecute(entry).Result);

            foreach (IBeforeSavingDbContextStrategy beforeSavingDbContextStrategy in beforeSavingDbContextStrategies)
                await beforeSavingDbContextStrategy.Execute(entry);
        }
    }

    /// <summary>
    /// Determine if an entity was modified
    /// </summary>
    /// <param name="entityEntry"></param>
    /// <returns></returns>
    protected virtual bool IsModifiedEntity(EntityEntry entityEntry)
    {
        var states = new List<EntityState>() { EntityState.Added, EntityState.Modified, EntityState.Deleted };

        return states.Contains(entityEntry.State) &&
               entityEntry.Entity is Entity;
    }
}