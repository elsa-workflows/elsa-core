using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.EntityFrameworkCore.Common.Contracts;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.Tenants.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
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
    public static string ElsaSchema { get; set; } = "Elsa";

    /// <inheritdoc/>
    public string Schema { get; }

    /// <summary>
    /// The table used to store the migrations history.
    /// </summary>
    public static string MigrationsHistoryTable { get; set; } = "__EFMigrationsHistory";

    /// <summary>
    /// Current TenantId of the user or of the background workflow.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Service Provider used in some strategies and filters.
    /// </summary>
    protected readonly IServiceProvider ServiceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElsaDbContextBase"/> class.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    protected ElsaDbContextBase(DbContextOptions options, IServiceProvider serviceProvider) : base(options)
    {
        ServiceProvider = serviceProvider;

        var elsaDbContextOptions = options.FindExtension<ElsaDbContextOptionsExtension>()?.Options;

        // ReSharper disable once VirtualMemberCallInConstructor
        Schema = !string.IsNullOrWhiteSpace(elsaDbContextOptions?.SchemaName) ? elsaDbContextOptions.SchemaName : ElsaSchema;

        var tenantAccessor = ServiceProvider.GetService<ITenantAccessor>();
        TenantId = tenantAccessor?.GetCurrentTenantId();
    }

    /// <inheritdoc/>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnBeforeSaving();
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

        // Add global filter on DbContext to split data between tenants
        var dbContextStrategies = ServiceProvider.GetServices<IDbContextStrategy>().ToList();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (dbContextStrategies != null && dbContextStrategies.Any())
            {
                IEnumerable<IModelCreatingDbContextStrategy> modelCreatingDbContextStrategies = dbContextStrategies!
                    .OfType<IModelCreatingDbContextStrategy>()
                    .Where(strategy => strategy.CanExecute(modelBuilder, entityType));

                foreach (IModelCreatingDbContextStrategy modelCreatingDbContextStrategy in modelCreatingDbContextStrategies)
                    modelCreatingDbContextStrategy.Execute(modelBuilder, entityType);
            }

            if (entityType.ClrType.IsAssignableTo(typeof(Entity)))
            {
                ParameterExpression parameter = Expression.Parameter(entityType.ClrType);

                Expression<Func<Entity, bool>> filterExpr = entity => TenantId == entity.TenantId;
                Expression body = ReplacingExpressionVisitor.Replace(filterExpr.Parameters[0], parameter, filterExpr.Body);
                LambdaExpression lambdaExpression = Expression.Lambda(body, parameter);

                entityType.SetQueryFilter(lambdaExpression);
            }
        }
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

    private void OnBeforeSaving()
    {
        var dbContextStrategies = ServiceProvider.GetServices<IDbContextStrategy>().ToList();
        foreach (EntityEntry entry in ChangeTracker.Entries().Where(IsModifiedEntity))
        {
            var beforeSavingDbContextStrategies = dbContextStrategies
                .OfType<IBeforeSavingDbContextStrategy>()
                .Where(strategy => strategy.CanExecute(entry));

            foreach (IBeforeSavingDbContextStrategy beforeSavingDbContextStrategy in beforeSavingDbContextStrategies)
                beforeSavingDbContextStrategy.Execute(entry);
        }
    }

    /// <summary>
    /// Determine if an entity was modified
    /// </summary>
    /// <param name="entityEntry"></param>
    /// <returns></returns>
    protected virtual bool IsModifiedEntity(EntityEntry entityEntry)
    {
        var states = new List<EntityState>()
        {
            EntityState.Added,
            EntityState.Modified,
            EntityState.Deleted
        };

        return states.Contains(entityEntry.State) &&
               entityEntry.Entity is Entity;
    }
}