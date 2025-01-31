using System.Linq.Expressions;
using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.EntityFrameworkCore.Oracle;

/// <summary>
/// Represents a class that handles entity model creation for SQLite databases.
/// </summary>
public class SetupForManagement : IEntityModelCreatingHandler
{
    private static Expression<Func<Version?, string?>> VersionToStringConverter => v => v != null ? v.ToString() : null;
    private static Expression<Func<string?, Version?>> StringToVersionConverter => v => v != null ? Version.Parse(v) : null;
    
    /// <inheritdoc />
    public void Handle(ElsaDbContextBase dbContext, ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if(!dbContext.Database.IsOracle())
            return;
        
        // In order to use data more than 2000 char we have to use NCLOB.
        // In Oracle, we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        modelBuilder.Entity<WorkflowInstance>().Property<string>("Data").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowInstance>().Ignore(x => x.WorkflowState);
        modelBuilder.Entity<WorkflowDefinition>().Ignore(x => x.CustomProperties);
        modelBuilder.Entity<WorkflowDefinition>().Ignore(x => x.Variables);
        modelBuilder.Entity<WorkflowDefinition>().Ignore(x => x.Inputs);
        modelBuilder.Entity<WorkflowDefinition>().Ignore(x => x.Outputs);
        modelBuilder.Entity<WorkflowDefinition>().Ignore(x => x.Outcomes);
        modelBuilder.Entity<WorkflowDefinition>().Ignore(x => x.Options);
        modelBuilder.Entity<WorkflowDefinition>().Property(x => x.ToolVersion).HasConversion(VersionToStringConverter, StringToVersionConverter);
        modelBuilder.Entity<WorkflowDefinition>().Property<string>("StringData").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowDefinition>().Property<string>("Data").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowDefinition>().Property(x => x.Description).HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowDefinition>().Property(x => x.MaterializerContext).HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowDefinition>().Property(x => x.BinaryData).HasColumnType("BLOB");
    }
}