using Elsa.Connections.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Connections.Credentials.Persistence.EFCore;

internal sealed class ConnectionCredentialUseGrantConfiguration : IEntityTypeConfiguration<ConnectionCredentialUseGrant>
{
    public void Configure(EntityTypeBuilder<ConnectionCredentialUseGrant> builder)
    {
        builder.ToTable("ConnectionCredentialUseGrants");
        builder.HasKey(x => new { x.TenantId, x.EnvironmentId, x.WorkflowInstanceId, x.LogicalBindingId });
        builder.Property(x => x.TenantId).HasMaxLength(200);
        builder.Property(x => x.EnvironmentId).HasMaxLength(200);
        builder.Property(x => x.WorkflowInstanceId).HasMaxLength(200);
        builder.Property(x => x.LogicalBindingId).HasMaxLength(200);
        builder.Property(x => x.ConnectionId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IssuedByActorId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BindingRevision).IsRequired();
        builder.Property(x => x.Revision).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.IssuedAt).IsRequired();
    }
}
