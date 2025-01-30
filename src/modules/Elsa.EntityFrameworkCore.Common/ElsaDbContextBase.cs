using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore;

/// <summary>
/// An optional base class to implement with some opinions on certain converters to install for certain DB providers.
/// </summary>
public abstract class ElsaDbContextBase : DbContext, IElsaDbContextSchema
{
    private static readonly ISet<EntityState> ModifiedEntityStates = new HashSet<EntityState>
    {
        EntityState.Added,
        EntityState.Modified,
    };

    protected readonly IServiceProvider ServiceProvider;
    public string? TenantId { get; set; }

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
    /// Initializes a new instance of the <see cref="ElsaDbContextBase"/> class.
    /// </summary>
    protected ElsaDbContextBase(DbContextOptions options, IServiceProvider serviceProvider) : base(options)
    {
        ServiceProvider = serviceProvider;
        var elsaDbContextOptions = options.FindExtension<ElsaDbContextOptionsExtension>()?.Options;

        // ReSharper disable once VirtualMemberCallInConstructor
        Schema = !string.IsNullOrWhiteSpace(elsaDbContextOptions?.SchemaName) ? elsaDbContextOptions.SchemaName : ElsaSchema;

        var tenantAccessor = serviceProvider.GetService<ITenantAccessor>();
        var tenantId = tenantAccessor?.Tenant?.Id;

        if (!string.IsNullOrWhiteSpace(tenantId))
            TenantId = tenantId;
    }

    /// <inheritdoc/>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await OnBeforeSavingAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
#if NET9_0_OR_GREATER
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
#endif
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (!string.IsNullOrWhiteSpace(Schema))
        {
            if (!Database.IsSqlite())
                modelBuilder.HasDefaultSchema(Schema);
        }

        var entityTypeHandlers = ServiceProvider.GetServices<IEntityModelCreatingHandler>().ToList();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            foreach (var handler in entityTypeHandlers)
            {
                handler.Handle(this, modelBuilder, entityType);
            }
        }
    }

    private async Task OnBeforeSavingAsync(CancellationToken cancellationToken)
    {
        var handlers = ServiceProvider.GetServices<IEntitySavingHandler>().ToList();
        foreach (var entry in ChangeTracker.Entries().Where(IsModifiedEntity))
        {
            foreach (var handler in handlers)
                await handler.HandleAsync(this, entry, cancellationToken);
        }
    }

    /// <summary>
    /// Determine if an entity was modified.
    /// </summary>
    private bool IsModifiedEntity(EntityEntry entityEntry)
    {
        return ModifiedEntityStates.Contains(entityEntry.State) && entityEntry.Entity is Entity;
    }
}