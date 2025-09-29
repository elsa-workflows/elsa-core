using Elsa.Identity.Entities;
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

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var config = new Configurations();
        modelBuilder.ApplyConfiguration<User>(config);
        modelBuilder.ApplyConfiguration<Application>(config);
        modelBuilder.ApplyConfiguration<Role>(config);
        
        base.OnModelCreating(modelBuilder);
    }
}