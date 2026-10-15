using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.ExternalAuthentication.Persistence.EFCore;

internal sealed class Configurations :
    IEntityTypeConfiguration<PersistedIdentityProviderConnection>,
    IEntityTypeConfiguration<PersistedExternalIdentityLink>,
    IEntityTypeConfiguration<PersistedBrokerTransaction>,
    IEntityTypeConfiguration<PersistedAuthorizationGrant>,
    IEntityTypeConfiguration<PersistedExternalAuthenticationSession>,
    IEntityTypeConfiguration<PersistedExternalAuthenticationRefreshToken>,
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
        builder.HasIndex(x => new { x.TenantId, x.ConnectionKey, x.Issuer, x.SubjectHash }).IsUnique().HasDatabaseName("IX_ExternalIdentityLink_Identity");
        // UserId references the identity aggregate, which lives in its own database context.
        // The link is resolved through IUserProvider/IUserStore rather than a foreign key.
        builder.HasIndex(x => new { x.TenantId, x.UserId }).HasDatabaseName("IX_ExternalIdentityLink_TenantId_UserId");
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
        builder.HasIndex(x => new { x.TenantId, x.UserId }).HasDatabaseName("IX_ExternalAuthenticationSession_TenantId_UserId");
        builder.HasIndex(x => x.ConnectionKey).HasDatabaseName("IX_ExternalAuthenticationSession_ConnectionKey");
    }

    public void Configure(EntityTypeBuilder<PersistedExternalAuthenticationRefreshToken> builder)
    {
        builder.ToTable("ExternalAuthenticationSessionRefreshTokens");
        builder.HasKey(x => x.SessionId);
        builder.HasIndex(x => x.Hash).IsUnique().HasDatabaseName("IX_ExternalAuthenticationSessionRefreshToken_Hash");
        builder.HasOne(x => x.Session).WithOne(x => x.RefreshToken).HasForeignKey<PersistedExternalAuthenticationRefreshToken>(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
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
