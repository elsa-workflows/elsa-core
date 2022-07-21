using Elsa.ActivityDefinitions.Entities;
using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.ActivityDefinitions.EntityFrameworkCore;

public class ActivityDefinitionsDbContext : ElsaDbContextBase
{
    public ActivityDefinitionsDbContext(DbContextOptions options) : base(options)
    {
    }
    
    public DbSet<ActivityDefinition> ActivityDefinitions { get; set; } = default!;

    protected override void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ActivityDefinitionsDbContext).Assembly);
    }
}