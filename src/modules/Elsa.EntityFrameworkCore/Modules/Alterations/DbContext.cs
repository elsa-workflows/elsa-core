﻿using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Models;
using Elsa.EntityFrameworkCore.Common;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Modules.Alterations;

/// <summary>
/// DB context for the runtime module.
/// </summary>
[UsedImplicitly]
public class AlterationsElsaDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public AlterationsElsaDbContext(DbContextOptions<AlterationsElsaDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }

    /// <summary>
    /// The alteration plans.
    /// </summary>
    public DbSet<AlterationPlan> AlterationPlans { get; set; } = default!;

    /// <summary>
    /// The alteration jobs.
    /// </summary>
    public DbSet<AlterationJob> AlterationJobs { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Ignore<AlterationLogEntry>();
        
        var config = new Configurations();
        modelBuilder.ApplyConfiguration<AlterationPlan>(config);
        modelBuilder.ApplyConfiguration<AlterationJob>(config);
    }
}