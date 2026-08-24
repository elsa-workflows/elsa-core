using Microsoft.EntityFrameworkCore;

namespace Elsa.UserTasks.Persistence.EFCore;

internal static class UserTaskEntityConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserTaskRecord>(entity =>
        {
            entity.ToTable("UserTasks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.WorkflowDefinitionId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.WorkflowDefinitionName).HasMaxLength(500);
            entity.Property(x => x.WorkflowInstanceId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.WorkflowInstanceReference).HasMaxLength(500);
            entity.Property(x => x.ActivityInstanceId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.BookmarkId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.MaterializationKey).HasMaxLength(900).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(4000);
            entity.Property(x => x.Reference).HasMaxLength(500);
            entity.Property(x => x.TaskType).HasMaxLength(200);
            entity.Property(x => x.TagsJson).IsRequired();
            entity.Property(x => x.RequesterProvider).HasMaxLength(200);
            entity.Property(x => x.RequesterType).HasMaxLength(64);
            entity.Property(x => x.RequesterId).HasMaxLength(450);
            entity.Property(x => x.RequesterDisplayName).HasMaxLength(500);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.MembershipResolutionMode).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.AssigneeProvider).HasMaxLength(200);
            entity.Property(x => x.AssigneeType).HasMaxLength(64);
            entity.Property(x => x.AssigneeId).HasMaxLength(450);
            entity.Property(x => x.AssigneeDisplayName).HasMaxLength(500);
            entity.Property(x => x.InstructionsJson).IsRequired();
            entity.Property(x => x.TaskDataJson).IsRequired();
            entity.Property(x => x.InvitationDefinitionsJson).IsRequired();
            entity.Property(x => x.HealthIssuesJson).IsRequired();
            entity.Property(x => x.HealthSeverity).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.HealthCode).HasMaxLength(200);
            entity.Property(x => x.HealthMessage).HasMaxLength(4000);
            entity.Property(x => x.Revision).IsConcurrencyToken().IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.MaterializationKey }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.BookmarkId }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Status });
            entity.HasIndex(x => new { x.TenantId, x.AssigneeProvider, x.AssigneeType, x.AssigneeId });
            entity.HasIndex(x => new { x.TenantId, x.Priority });
            entity.HasIndex(x => new { x.TenantId, x.DueAt });
            entity.HasIndex(x => new { x.TenantId, x.WorkflowDefinitionId });
            entity.HasIndex(x => new { x.TenantId, x.WorkflowInstanceId });
            entity.HasIndex(x => new { x.TenantId, x.ActivityInstanceId });
            entity.HasIndex(x => new { x.TenantId, x.CreatedAt });
            entity.HasIndex(x => new { x.TenantId, x.CompletedAt });
        });

        modelBuilder.Entity<UserTaskCandidateRecord>(entity =>
        {
            entity.ToTable("UserTaskCandidates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.TaskId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Provider).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ParticipantKey).HasMaxLength(800).IsRequired();
            entity.Property(x => x.ParticipantType).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.ParticipantId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(500);
            entity.Property(x => x.Source).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.SourceGroupProvider).HasMaxLength(200);
            entity.Property(x => x.SourceGroupId).HasMaxLength(450);
            entity.HasIndex(x => new { x.TenantId, x.TaskId, x.Provider, x.ParticipantType, x.ParticipantId, x.Source }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Provider, x.ParticipantType, x.ParticipantId });
            entity.HasIndex(x => new { x.TenantId, x.TaskId, x.ParticipantKey });
            entity.HasIndex(x => new { x.TenantId, x.TaskId });
        });

        modelBuilder.Entity<UserTaskSnapshotMemberRecord>(entity =>
        {
            entity.ToTable("UserTaskSnapshotMembers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.TaskId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Provider).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ParticipantKey).HasMaxLength(800).IsRequired();
            entity.Property(x => x.ParticipantType).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.ParticipantId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.SourceGroupProvider).HasMaxLength(200);
            entity.Property(x => x.SourceGroupId).HasMaxLength(450);
            entity.HasIndex(x => new { x.TenantId, x.TaskId, x.Provider, x.ParticipantType, x.ParticipantId }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Provider, x.ParticipantType, x.ParticipantId });
            entity.HasIndex(x => new { x.TenantId, x.TaskId, x.ParticipantKey });
        });

        modelBuilder.Entity<UserTaskExclusionRecord>(entity =>
        {
            entity.ToTable("UserTaskExclusions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.TaskId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Provider).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ParticipantKey).HasMaxLength(800).IsRequired();
            entity.Property(x => x.ParticipantType).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.ParticipantId).HasMaxLength(450).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.TaskId, x.Provider, x.ParticipantType, x.ParticipantId }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Provider, x.ParticipantType, x.ParticipantId });
            entity.HasIndex(x => new { x.TenantId, x.TaskId, x.ParticipantKey });
        });

        modelBuilder.Entity<UserTaskEventRecord>(entity =>
        {
            entity.ToTable("UserTaskEvents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.TaskId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.OperationId).HasMaxLength(450);
            entity.Property(x => x.ActorProvider).HasMaxLength(200);
            entity.Property(x => x.ActorType).HasMaxLength(64);
            entity.Property(x => x.ActorId).HasMaxLength(450);
            entity.Property(x => x.Reason).HasMaxLength(4000);
            entity.Property(x => x.MetadataJson).IsRequired();
            // Audit is append-only and several entries may share a revision (an audited read does not
            // consume the concurrency token), so this is a lookup index rather than a uniqueness constraint.
            entity.HasIndex(x => new { x.TaskId, x.Revision });
            entity.HasIndex(x => new { x.TenantId, x.TaskId, x.OccurredAt });
        });

        modelBuilder.Entity<UserTaskOperationRecord>(entity =>
        {
            entity.ToTable("UserTaskOperations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.TaskId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.OperationId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Kind).HasMaxLength(64).IsRequired();
            entity.Property(x => x.RequestHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.ActionKey).HasMaxLength(200);
            entity.Property(x => x.ErrorCode).HasMaxLength(200);
            entity.HasIndex(x => new { x.TenantId, x.TaskId, x.OperationId }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Status, x.UpdatedAt });
        });

        modelBuilder.Entity<UserTaskInvitationRecord>(entity =>
        {
            entity.ToTable("UserTaskInvitations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.TaskId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.SiblingGroupId).HasMaxLength(450);
            entity.Property(x => x.TokenHash).HasMaxLength(256).IsRequired();
            entity.Property(x => x.VerifierProvider).HasMaxLength(200).IsRequired();
            entity.Property(x => x.AllowedActionsJson).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.TaskId, x.Status });
            entity.HasIndex(x => new { x.TenantId, x.Status, x.ExpiresAt });
        });

        modelBuilder.Entity<UserTaskInvitationDeliveryRecord>(entity =>
        {
            entity.ToTable("UserTaskInvitationDeliveries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.TaskId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.InvitationId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.DispatcherProvider).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EncryptedToken).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.LastErrorCode).HasMaxLength(128);
            entity.HasIndex(x => new { x.TenantId, x.Status, x.AvailableAt });
            entity.HasIndex(x => new { x.TenantId, x.InvitationId }).IsUnique();
        });

        modelBuilder.Entity<UserTaskGuestSessionRecord>(entity =>
        {
            entity.ToTable("UserTaskGuestSessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.TaskId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.InvitationId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.SessionTokenHash).HasMaxLength(256).IsRequired();
            entity.Property(x => x.GuestParticipantJson).IsRequired();
            entity.Property(x => x.CapabilitiesJson).IsRequired();
            entity.HasIndex(x => x.SessionTokenHash).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.TaskId, x.ExpiresAt });
        });
    }
}
