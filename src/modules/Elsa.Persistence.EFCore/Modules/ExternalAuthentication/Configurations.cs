using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Elsa.Identity.Entities;

namespace Elsa.Persistence.EFCore.Modules.ExternalAuthentication;

internal sealed class Configurations :
    IEntityTypeConfiguration<PersistedIdentityProviderConnection>,
    IEntityTypeConfiguration<PersistedExternalIdentityLink>,
    IEntityTypeConfiguration<PersistedAuthenticationClient>,
    IEntityTypeConfiguration<PersistedBrokerTransaction>,
    IEntityTypeConfiguration<PersistedAuthorizationGrant>,
    IEntityTypeConfiguration<PersistedExternalAuthenticationSession>,
    IEntityTypeConfiguration<PersistedConnectionObservation>,
    IEntityTypeConfiguration<PersistedPreviewResult>,
    IEntityTypeConfiguration<ExternalAuthenticationRegistryVersion>
{
    public void Configure(EntityTypeBuilder<PersistedIdentityProviderConnection> builder)
    {
        builder.ToTable("IdentityProviderConnections");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.Key }).IsUnique().HasDatabaseName("IX_IdentityProviderConnection_TenantId_Key");
        builder.HasIndex(x => x.MaterialRevision).HasDatabaseName("IX_IdentityProviderConnection_MaterialRevision");
        builder.Property(x => x.Revision).IsConcurrencyToken();
    }

    public void Configure(EntityTypeBuilder<PersistedExternalIdentityLink> builder)
    {
        builder.ToTable("ExternalIdentityLinks");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.ConnectionId, x.Issuer, x.SubjectHash }).IsUnique().HasDatabaseName("IX_ExternalIdentityLink_Identity");
        builder.HasIndex(x => new { x.TenantId, x.UserId }).HasDatabaseName("IX_ExternalIdentityLink_TenantId_UserId");
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<PersistedAuthenticationClient> builder)
    {
        builder.ToTable("ExternalAuthenticationClients");
        builder.HasKey(x => x.ClientId);
    }

    public void Configure(EntityTypeBuilder<PersistedBrokerTransaction> builder)
    {
        builder.ToTable("ExternalAuthenticationBrokerTransactions");
        builder.HasKey(x => new { x.Purpose, x.HandleHash });
        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("IX_ExternalAuthenticationBrokerTransaction_ExpiresAt");
    }

    public void Configure(EntityTypeBuilder<PersistedAuthorizationGrant> builder)
    {
        builder.ToTable("ExternalAuthenticationAuthorizationGrants");
        builder.HasKey(x => x.CodeHash);
        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("IX_ExternalAuthenticationAuthorizationGrant_ExpiresAt");
    }

    public void Configure(EntityTypeBuilder<PersistedExternalAuthenticationSession> builder)
    {
        builder.ToTable("ExternalAuthenticationSessions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CurrentRefreshTokenHash).IsUnique().HasDatabaseName("IX_ExternalAuthenticationSession_RefreshTokenHash");
        builder.HasIndex(x => new { x.TenantId, x.UserId }).HasDatabaseName("IX_ExternalAuthenticationSession_TenantId_UserId");
        builder.HasIndex(x => x.ConnectionId).HasDatabaseName("IX_ExternalAuthenticationSession_ConnectionId");
    }

    public void Configure(EntityTypeBuilder<PersistedConnectionObservation> builder)
    {
        builder.ToTable("ExternalAuthenticationConnectionObservations");
        builder.HasKey(x => x.ConnectionId);
    }

    public void Configure(EntityTypeBuilder<PersistedPreviewResult> builder)
    {
        builder.ToTable("ExternalAuthenticationPreviewResults");
        builder.HasKey(x => x.HandleHash);
        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("IX_ExternalAuthenticationPreviewResult_ExpiresAt");
    }

    public void Configure(EntityTypeBuilder<ExternalAuthenticationRegistryVersion> builder)
    {
        builder.ToTable("ExternalAuthenticationRegistryVersions");
        builder.HasKey(x => x.Id);
    }
}
