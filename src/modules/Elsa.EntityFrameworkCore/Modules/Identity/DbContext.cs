using Elsa.EntityFrameworkCore.Common;
using Elsa.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Modules.Identity;

/// <summary>
/// The database context for the Identity module.
/// </summary>
public class IdentityElsaDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public IdentityElsaDbContext(DbContextOptions<IdentityElsaDbContext> options, IServiceProvider serviceProvider) : base(options)
    {
        var elsaDbContextOptions = options.FindExtension<ElsaDbContextOptionsExtension>()?.Options;
        _additionnalEntityConfigurations = elsaDbContextOptions?.AdditionnalEntityConfigurations;
        _serviceProvider = serviceProvider;
    }

    private readonly Action<ModelBuilder, IServiceProvider>? _additionnalEntityConfigurations;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The users.
    /// </summary>
    public DbSet<User> Users { get; set; } = default!;

    /// <summary>
    /// The applications.
    /// </summary>
    public DbSet<Application> Applications { get; set; } = default!;

    /// <summary>
    /// The roles.
    /// </summary>
    public DbSet<Role> Roles { get; set; } = default!;

    /// <inheritdoc />
    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        var config = new Configurations();
        modelBuilder.ApplyConfiguration<User>(config);
        modelBuilder.ApplyConfiguration<Application>(config);
        modelBuilder.ApplyConfiguration<Role>(config);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _additionnalEntityConfigurations?.Invoke(modelBuilder, _serviceProvider);

        base.OnModelCreating(modelBuilder);
    }
}