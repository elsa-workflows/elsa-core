using Elsa.ActivityDefinitions.Entities;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.ActivityDefinitions;

public class ActivityDefinitionsDbContext : DbContextBase
{
    public ActivityDefinitionsDbContext(DbContextOptions options) : base(options)
    {
    }
    
    public DbSet<ActivityDefinition> ActivityDefinitions { get; set; } = default!;


    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations());
    }
}