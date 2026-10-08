using Elsa.ExternalAuthentication.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.Oracle.Configurations;

/// <summary>
/// Widens the serialized and protected columns to Oracle LOB types.
/// </summary>
/// <remarks>
/// Oracle infers <c>NVARCHAR2(2000)</c> for <see cref="string"/> and <c>RAW(2000)</c> for <see cref="byte"/> arrays.
/// Serialized adapter settings, claim projections and data-protected payloads routinely exceed that, so they are
/// mapped to <c>NCLOB</c>/<c>BLOB</c> instead. Indexed columns (hashes, keys, revisions) are deliberately left alone,
/// because Oracle cannot index a LOB.
/// </remarks>
public class ExternalAuthenticationOracleConfigurations :
    IEntityTypeConfiguration<PersistedIdentityProviderConnection>,
    IEntityTypeConfiguration<PersistedBrokerTransaction>,
    IEntityTypeConfiguration<PersistedExternalAuthenticationSession>,
    IEntityTypeConfiguration<PersistedConnectionObservation>,
    IEntityTypeConfiguration<PersistedPreviewResult>
{
    private const string Text = "NCLOB";
    private const string Binary = "BLOB";

    public void Configure(EntityTypeBuilder<PersistedIdentityProviderConnection> builder)
    {
        builder.Property(x => x.AdapterSettingsJson).HasColumnType(Text);
        builder.Property(x => x.SecretBindingsJson).HasColumnType(Text);
        builder.Property(x => x.UnlinkedPolicyJson).HasColumnType(Text);
        builder.Property(x => x.PermissionGrantSourcesJson).HasColumnType(Text);
        builder.Property(x => x.ClaimProjectionJson).HasColumnType(Text);
    }

    public void Configure(EntityTypeBuilder<PersistedBrokerTransaction> builder)
    {
        builder.Property(x => x.ProtectedPayload).HasColumnType(Binary);
    }

    public void Configure(EntityTypeBuilder<PersistedExternalAuthenticationSession> builder)
    {
        builder.Property(x => x.ExternalGrantsJson).HasColumnType(Text);
        builder.Property(x => x.ProtectedUpstreamLogoutHint).HasColumnType(Binary);
    }

    public void Configure(EntityTypeBuilder<PersistedConnectionObservation> builder)
    {
        builder.Property(x => x.Summary).HasColumnType(Text);
        builder.Property(x => x.WarningsJson).HasColumnType(Text);
    }

    public void Configure(EntityTypeBuilder<PersistedPreviewResult> builder)
    {
        builder.Property(x => x.ProjectedClaimsJson).HasColumnType(Text);
        builder.Property(x => x.PermissionProjectionJson).HasColumnType(Text);
        builder.Property(x => x.WarningsJson).HasColumnType(Text);
    }
}
