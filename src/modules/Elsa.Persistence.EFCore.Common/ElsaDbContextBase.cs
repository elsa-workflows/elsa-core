using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore;

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

    protected IServiceProvider ServiceProvider { get; }
    private readonly ElsaDbContextOptions? _elsaDbContextOptions;
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
        _elsaDbContextOptions = options.FindExtension<ElsaDbContextOptionsExtension>()?.Options;

        // ReSharper disable once VirtualMemberCallInConstructor
        Schema = !string.IsNullOrWhiteSpace(_elsaDbContextOptions?.SchemaName) ? _elsaDbContextOptions.SchemaName : ElsaSchema;

        var tenantAccessor = serviceProvider.GetService<ITenantAccessor>();
        var tenantId = tenantAccessor?.Tenant?.Id;

        if (!string.IsNullOrWhiteSpace(tenantId))
            TenantId = tenantId.NullIfEmpty();
    }

    /// <inheritdoc/>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await OnBeforeSavingAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (!string.IsNullOrWhiteSpace(Schema))
            modelBuilder.HasDefaultSchema(Schema);

        var additionalConfigurations = _elsaDbContextOptions?.GetModelConfigurations(this);

        additionalConfigurations?.Invoke(modelBuilder);

        using var scope = ServiceProvider.CreateScope();
        var entityTypeHandlers = scope.ServiceProvider.GetServices<IEntityModelCreatingHandler>().ToList();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            foreach (var handler in entityTypeHandlers)
                handler.Handle(this, modelBuilder, entityType);
        }
    }

    private async Task OnBeforeSavingAsync(CancellationToken cancellationToken)
    {
        using var scope = ServiceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IEntitySavingHandler>().ToList();
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