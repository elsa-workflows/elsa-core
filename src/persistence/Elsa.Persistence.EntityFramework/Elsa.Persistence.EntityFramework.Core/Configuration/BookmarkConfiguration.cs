using Elsa.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFramework.Core.Configuration
{
    public class BookmarkConfiguration : IEntityTypeConfiguration<Bookmark>
    {
        public void Configure(EntityTypeBuilder<Bookmark> builder)
        {
            builder.HasIndex(x => new { x.ActivityType, x.TenantId, x.Hash}).HasDatabaseName($"IX_{nameof(Bookmark)}_{nameof(Bookmark.ActivityType)}_{nameof(Bookmark.TenantId)}_{nameof(Bookmark.Hash)}");
            builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(Bookmark)}_{nameof(Bookmark.TenantId)}");
            builder.HasIndex(x => x.Hash).HasDatabaseName($"IX_{nameof(Bookmark)}_{nameof(Bookmark.Hash)}");
            builder.HasIndex(x => x.ActivityType).HasDatabaseName($"IX_{nameof(Bookmark)}_{nameof(Bookmark.ActivityType)}");
            builder.HasIndex(x => x.ActivityId).HasDatabaseName($"IX_{nameof(Bookmark)}_{nameof(Bookmark.ActivityId)}");
            builder.HasIndex(x => x.WorkflowInstanceId).HasDatabaseName($"IX_{nameof(Bookmark)}_{nameof(Bookmark.WorkflowInstanceId)}");
            builder.HasIndex(x => x.CorrelationId).HasDatabaseName($"IX_{nameof(Bookmark)}_{nameof(Bookmark.CorrelationId)}");
            builder.HasIndex(x => new {x.Hash, x.CorrelationId, x.TenantId}).HasDatabaseName($"IX_{nameof(Bookmark)}_{nameof(Bookmark.Hash)}_{nameof(Bookmark.CorrelationId)}_{nameof(Bookmark.TenantId)}");
        }
    }
}
