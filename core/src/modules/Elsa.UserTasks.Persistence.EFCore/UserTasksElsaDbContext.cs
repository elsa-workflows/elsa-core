using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Elsa.UserTasks.Persistence.EFCore;

/// <summary>
/// Persistence-side User Tasks context. Aggregate-safe JSON stays alongside normalized relationships so
/// every relational provider can share the same tenant-scoped schema.
/// </summary>
public class UserTasksElsaDbContext : ElsaDbContextBase
{
    public UserTasksElsaDbContext(DbContextOptions<UserTasksElsaDbContext> options, IServiceProvider serviceProvider)
        : base(options, serviceProvider)
    {
    }

    public DbSet<UserTaskRecord> UserTasks => Set<UserTaskRecord>();
    public DbSet<UserTaskCandidateRecord> UserTaskCandidates => Set<UserTaskCandidateRecord>();
    public DbSet<UserTaskSnapshotMemberRecord> UserTaskSnapshotMembers => Set<UserTaskSnapshotMemberRecord>();
    public DbSet<UserTaskExclusionRecord> UserTaskExclusions => Set<UserTaskExclusionRecord>();
    public DbSet<UserTaskEventRecord> UserTaskEvents => Set<UserTaskEventRecord>();
    public DbSet<UserTaskOperationRecord> UserTaskOperations => Set<UserTaskOperationRecord>();
    public DbSet<UserTaskInvitationRecord> UserTaskInvitations => Set<UserTaskInvitationRecord>();
    public DbSet<UserTaskInvitationDeliveryRecord> UserTaskInvitationDeliveries => Set<UserTaskInvitationDeliveryRecord>();
    public DbSet<UserTaskGuestSessionRecord> UserTaskGuestSessions => Set<UserTaskGuestSessionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        UserTaskEntityConfiguration.Configure(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
