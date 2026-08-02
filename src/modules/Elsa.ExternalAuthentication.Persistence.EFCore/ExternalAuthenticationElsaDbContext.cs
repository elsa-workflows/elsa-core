using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Elsa.ExternalAuthentication.Persistence.EFCore;

/// <summary>
/// The database context for the External Authentication module.
/// </summary>
public class ExternalAuthenticationElsaDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public ExternalAuthenticationElsaDbContext(DbContextOptions<ExternalAuthenticationElsaDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }

    /// <summary>
    /// The durable identity provider connections.
    /// </summary>
    public DbSet<PersistedIdentityProviderConnection> IdentityProviderConnections { get; set; } = null!;

    /// <summary>
    /// The links between external identities and Elsa users.
    /// </summary>
    public DbSet<PersistedExternalIdentityLink> ExternalIdentityLinks { get; set; } = null!;

    /// <summary>
    /// The in-flight broker transactions.
    /// </summary>
    public DbSet<PersistedBrokerTransaction> ExternalAuthenticationBrokerTransactions { get; set; } = null!;

    /// <summary>
    /// The issued authorization grants.
    /// </summary>
    public DbSet<PersistedAuthorizationGrant> ExternalAuthenticationAuthorizationGrants { get; set; } = null!;

    /// <summary>
    /// The external authentication sessions.
    /// </summary>
    public DbSet<PersistedExternalAuthenticationSession> ExternalAuthenticationSessions { get; set; } = null!;

    /// <summary>
    /// The latest connection test observations.
    /// </summary>
    public DbSet<PersistedConnectionObservation> ExternalAuthenticationConnectionObservations { get; set; } = null!;

    /// <summary>
    /// The pending connection preview results.
    /// </summary>
    public DbSet<PersistedPreviewResult> ExternalAuthenticationPreviewResults { get; set; } = null!;

    /// <summary>
    /// The connection registry version used to invalidate caches across nodes.
    /// </summary>
    public DbSet<ExternalAuthenticationRegistryVersion> ExternalAuthenticationRegistryVersions { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var configurations = new Configurations();
        modelBuilder.ApplyConfiguration<PersistedIdentityProviderConnection>(configurations);
        modelBuilder.ApplyConfiguration<PersistedExternalIdentityLink>(configurations);
        modelBuilder.ApplyConfiguration<PersistedBrokerTransaction>(configurations);
        modelBuilder.ApplyConfiguration<PersistedAuthorizationGrant>(configurations);
        modelBuilder.ApplyConfiguration<PersistedExternalAuthenticationSession>(configurations);
        modelBuilder.ApplyConfiguration<PersistedConnectionObservation>(configurations);
        modelBuilder.ApplyConfiguration<PersistedPreviewResult>(configurations);
        modelBuilder.ApplyConfiguration<ExternalAuthenticationRegistryVersion>(configurations);
        base.OnModelCreating(modelBuilder);
    }
}
