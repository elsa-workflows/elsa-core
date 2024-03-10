﻿using System.Diagnostics.CodeAnalysis;
using Elsa.Common.Entities;
using Elsa.EntityFrameworkCore.Common.Contracts;
using Elsa.Tenants.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Common;

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
    
    /// <summary>
    /// The default schema used by Elsa.
    /// </summary>
    public static string ElsaSchema { get; set; } = "Elsa";

    /// <inheritdoc/>
    public string Schema { get; }

    /// <summary>
    /// The table used to store the migrations' history.
    /// </summary>
    public static string MigrationsHistoryTable { get; set; } = "__EFMigrationsHistory";

    /// <summary>
    /// Current TenantId.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Service Provider used in some strategies and filters.
    /// </summary>
    protected readonly IServiceProvider ServiceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElsaDbContextBase"/> class.
    /// </summary>
    [RequiresUnreferencedCode("The constructor of the base class is called by the generated code.")]
    protected ElsaDbContextBase(DbContextOptions options, IServiceProvider serviceProvider) : base(options)
    {
        ServiceProvider = serviceProvider;

        var elsaDbContextOptions = options.FindExtension<ElsaDbContextOptionsExtension>()?.Options;

        // ReSharper disable once VirtualMemberCallInConstructor
        Schema = !string.IsNullOrWhiteSpace(elsaDbContextOptions?.SchemaName) ? elsaDbContextOptions.SchemaName : ElsaSchema;

        var tenantAccessor = ServiceProvider.GetService<ITenantResolver>();
        //TenantId = tenantAccessor?.GetTenantAsync();
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
        {
            if (!Database.IsSqlite())
                modelBuilder.HasDefaultSchema(Schema);
        }
   
        var modelHandlers = ServiceProvider.GetServices<IModelCreatingHandler>();
        
        foreach (var handler in modelHandlers) 
            handler.Handle(this, modelBuilder);
        
        var entityTypeHandlers = ServiceProvider.GetServices<IEntityModelCreatingHandler>().ToList();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
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