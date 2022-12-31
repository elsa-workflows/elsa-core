using Elsa.ActivityDefinitions.Entities;
using Elsa.EntityFrameworkCore.Common;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Modules.ActivityDefinitions;

public class ActivityDefinitionsElsaDbContext : ElsaDbContextBase
{
    public ActivityDefinitionsElsaDbContext(DbContextOptions options) : base(options)
    {
    }
    
    public DbSet<ActivityDefinition> ActivityDefinitions { get; set; } = default!;


    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations());
    }
}