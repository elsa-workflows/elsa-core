using Elsa.CustomActivities.Entities;
using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.CustomActivities.EntityFrameworkCore;

public class CustomActivitiesDbContext : ElsaDbContextBase
{
    public CustomActivitiesDbContext(DbContextOptions options) : base(options)
    {
    }
    
    public DbSet<ActivityDefinition> ActivityDefinitions { get; set; } = default!;

    protected override void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomActivitiesDbContext).Assembly);
    }
}