using Elsa.WorkflowSettings.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.Core.Configuration
{
    public class WorkflowSettingConfiguration : IEntityTypeConfiguration<WorkflowSetting>
    {
        public void Configure(EntityTypeBuilder<WorkflowSetting> builder)
        {
            builder.HasIndex(x => x.WorkflowBlueprintId).HasDatabaseName($"IX_{nameof(WorkflowSetting)}_{nameof(WorkflowSetting.WorkflowBlueprintId)}");
            builder.HasIndex(x => x.Key).HasDatabaseName($"IX_{nameof(WorkflowSetting)}_{nameof(WorkflowSetting.Key)}");
            builder.HasIndex(x => x.Value).HasDatabaseName($"IX_{nameof(WorkflowSetting)}_{nameof(WorkflowSetting.Value)}");
        }
    }
}