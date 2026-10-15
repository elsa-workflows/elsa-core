using Elsa.Connections.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Connections.Credentials.Persistence.EFCore;

internal sealed class ConnectionCredentialBindingConfiguration : IEntityTypeConfiguration<ConnectionCredentialBinding>
{
    public void Configure(EntityTypeBuilder<ConnectionCredentialBinding> builder)
    {
        builder.ToTable("ConnectionCredentialBindings");
        builder.HasKey(x => new { x.TenantId, x.EnvironmentId, x.LogicalBindingId });
        builder.Property(x => x.TenantId).HasMaxLength(200);
        builder.Property(x => x.EnvironmentId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LogicalBindingId).HasMaxLength(200);
        builder.Property(x => x.ConnectionId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Revision).IsRequired();
    }
}
