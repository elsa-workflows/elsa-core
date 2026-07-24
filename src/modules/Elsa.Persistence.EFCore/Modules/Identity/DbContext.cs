using Elsa.Identity.Entities;
using Elsa.Persistence.EFCore.Modules.ExternalAuthentication;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Modules.Identity;

/// <summary>
/// The database context for the Identity module.
/// </summary>
public class IdentityElsaDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public IdentityElsaDbContext(DbContextOptions<IdentityElsaDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }

    /// <summary>
    /// The users.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// The applications.
    /// </summary>
    public DbSet<Application> Applications { get; set; } = null!;

    /// <summary>
    /// The roles.
    /// </summary>
    public DbSet<Role> Roles { get; set; } = null!;

    /// <summary>
    /// Database-owned external authentication connections.
    /// </summary>
    public DbSet<PersistedIdentityProviderConnection> IdentityProviderConnections { get; set; } = null!;

    public DbSet<PersistedExternalIdentityLink> ExternalIdentityLinks { get; set; } = null!;
    public DbSet<PersistedAuthenticationClient> ExternalAuthenticationClients { get; set; } = null!;
    public DbSet<PersistedBrokerTransaction> ExternalAuthenticationBrokerTransactions { get; set; } = null!;
    public DbSet<PersistedAuthorizationGrant> ExternalAuthenticationAuthorizationGrants { get; set; } = null!;
    public DbSet<PersistedExternalAuthenticationSession> ExternalAuthenticationSessions { get; set; } = null!;
    public DbSet<PersistedConnectionObservation> ExternalAuthenticationConnectionObservations { get; set; } = null!;
    public DbSet<PersistedPreviewResult> ExternalAuthenticationPreviewResults { get; set; } = null!;
    public DbSet<ExternalAuthenticationRegistryVersion> ExternalAuthenticationRegistryVersions { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var config = new Configurations();
        modelBuilder.ApplyConfiguration<User>(config);
        modelBuilder.ApplyConfiguration<Application>(config);
        modelBuilder.ApplyConfiguration<Role>(config);
        var externalAuthenticationConfig = new Elsa.Persistence.EFCore.Modules.ExternalAuthentication.Configurations();
        modelBuilder.ApplyConfiguration<PersistedIdentityProviderConnection>(externalAuthenticationConfig);
        modelBuilder.ApplyConfiguration<PersistedExternalIdentityLink>(externalAuthenticationConfig);
        modelBuilder.ApplyConfiguration<PersistedAuthenticationClient>(externalAuthenticationConfig);
        modelBuilder.ApplyConfiguration<PersistedBrokerTransaction>(externalAuthenticationConfig);
        modelBuilder.ApplyConfiguration<PersistedAuthorizationGrant>(externalAuthenticationConfig);
        modelBuilder.ApplyConfiguration<PersistedExternalAuthenticationSession>(externalAuthenticationConfig);
        modelBuilder.ApplyConfiguration<PersistedConnectionObservation>(externalAuthenticationConfig);
        modelBuilder.ApplyConfiguration<PersistedPreviewResult>(externalAuthenticationConfig);
        modelBuilder.ApplyConfiguration<ExternalAuthenticationRegistryVersion>(externalAuthenticationConfig);
        
        base.OnModelCreating(modelBuilder);
    }
}
