using Elsa.DataSets.Entities;
using Elsa.EntityFrameworkCore.Common;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Modules.DataSets;

/// <summary>
/// The database context for the Identity module.
/// </summary>
public class DataSetElsaDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public DataSetElsaDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// The data set definitions.
    /// </summary>
    public DbSet<DataSetDefinition> DataSetDefinitions { get; set; } = default!;
    
    /// <summary>
    /// The linked service definitions.
    /// </summary>
    public DbSet<LinkedServiceDefinition> LinkedServiceDefinitions { get; set; } = default!;
    
    /// <inheritdoc />
    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        var config = new Configurations();
        modelBuilder.ApplyConfiguration<DataSetDefinition>(config);
        modelBuilder.ApplyConfiguration<LinkedServiceDefinition>(config);
    }
}