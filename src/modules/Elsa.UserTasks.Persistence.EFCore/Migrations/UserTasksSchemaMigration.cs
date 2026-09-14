using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.UserTasks.Persistence.EFCore.Migrations;

/// <summary>
/// Provider-neutral initial schema operations. Provider migration assemblies call this helper so their
/// generated migration shape stays functionally identical while EF chooses provider column types.
/// </summary>
public static class UserTasksSchemaMigration
{
    public static void Up(MigrationBuilder migrationBuilder, string schema)
    {
        migrationBuilder.EnsureSchema(schema);

        migrationBuilder.CreateTable(name: "UserTasks", schema: schema,
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 450, nullable: false),
                TenantId = table.Column<string>(maxLength: 450, nullable: false),
                WorkflowDefinitionId = table.Column<string>(maxLength: 450, nullable: false),
                WorkflowDefinitionName = table.Column<string>(maxLength: 500, nullable: true),
                WorkflowDefinitionVersion = table.Column<int>(nullable: true),
                WorkflowInstanceId = table.Column<string>(maxLength: 450, nullable: false),
                WorkflowInstanceReference = table.Column<string>(maxLength: 500, nullable: true),
                ActivityInstanceId = table.Column<string>(maxLength: 450, nullable: false),
                BookmarkId = table.Column<string>(maxLength: 450, nullable: false),
                MaterializationKey = table.Column<string>(maxLength: 900, nullable: false),
                Title = table.Column<string>(maxLength: 500, nullable: false),
                Summary = table.Column<string>(maxLength: 4000, nullable: true),
                Reference = table.Column<string>(maxLength: 500, nullable: true),
                TaskType = table.Column<string>(maxLength: 200, nullable: true),
                TagsJson = table.Column<string>(nullable: false),
                RequesterProvider = table.Column<string>(maxLength: 200, nullable: true),
                RequesterType = table.Column<string>(maxLength: 64, nullable: true),
                RequesterId = table.Column<string>(maxLength: 450, nullable: true),
                RequesterDisplayName = table.Column<string>(maxLength: 500, nullable: true),
                Priority = table.Column<int>(nullable: false),
                DueAt = table.Column<DateTimeOffset>(nullable: true),
                IsOverdue = table.Column<bool>(nullable: false),
                Status = table.Column<string>(maxLength: 32, nullable: false),
                TimeoutEnabled = table.Column<bool>(nullable: false),
                CancellationEnabled = table.Column<bool>(nullable: false),
                AllowManagerExclusionOverride = table.Column<bool>(nullable: false),
                MembershipResolutionMode = table.Column<string>(maxLength: 32, nullable: true),
                AssigneeProvider = table.Column<string>(maxLength: 200, nullable: true),
                AssigneeType = table.Column<string>(maxLength: 64, nullable: true),
                AssigneeId = table.Column<string>(maxLength: 450, nullable: true),
                AssigneeDisplayName = table.Column<string>(maxLength: 500, nullable: true),
                InstructionsJson = table.Column<string>(nullable: false),
                TaskDataJson = table.Column<string>(nullable: false),
                FormReferenceJson = table.Column<string>(nullable: true),
                PinnedFormJson = table.Column<string>(nullable: true),
                ActionsJson = table.Column<string>(nullable: true),
                InvitationDefinitionsJson = table.Column<string>(nullable: false),
                HealthIssuesJson = table.Column<string>(nullable: false),
                HealthSeverity = table.Column<string>(maxLength: 32, nullable: true),
                HealthCode = table.Column<string>(maxLength: 200, nullable: true),
                HealthMessage = table.Column<string>(maxLength: 4000, nullable: true),
                CompletionActionKey = table.Column<string>(maxLength: 200, nullable: true),
                CompletionDataJson = table.Column<string>(nullable: true),
                CompletionActorJson = table.Column<string>(nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(nullable: false),
                AssignedAt = table.Column<DateTimeOffset>(nullable: true),
                CompletedAt = table.Column<DateTimeOffset>(nullable: true),
                Revision = table.Column<int>(nullable: false),
                CreatedFromBookmarkRevision = table.Column<long>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_UserTasks", x => x.Id));

        migrationBuilder.CreateTable(name: "UserTaskCandidates", schema: schema,
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 450, nullable: false),
                TenantId = table.Column<string>(maxLength: 450, nullable: false),
                TaskId = table.Column<string>(maxLength: 450, nullable: false),
                Provider = table.Column<string>(maxLength: 200, nullable: false),
                ParticipantKey = table.Column<string>(maxLength: 800, nullable: false),
                ParticipantType = table.Column<string>(maxLength: 32, nullable: false),
                ParticipantId = table.Column<string>(maxLength: 450, nullable: false),
                DisplayName = table.Column<string>(maxLength: 500, nullable: true),
                Source = table.Column<string>(maxLength: 32, nullable: false),
                SourceGroupProvider = table.Column<string>(maxLength: 200, nullable: true),
                SourceGroupId = table.Column<string>(maxLength: 450, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_UserTaskCandidates", x => x.Id));

        migrationBuilder.CreateTable(name: "UserTaskSnapshotMembers", schema: schema,
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 450, nullable: false),
                TenantId = table.Column<string>(maxLength: 450, nullable: false),
                TaskId = table.Column<string>(maxLength: 450, nullable: false),
                Provider = table.Column<string>(maxLength: 200, nullable: false),
                ParticipantKey = table.Column<string>(maxLength: 800, nullable: false),
                ParticipantType = table.Column<string>(maxLength: 32, nullable: false),
                ParticipantId = table.Column<string>(maxLength: 450, nullable: false),
                SourceGroupProvider = table.Column<string>(maxLength: 200, nullable: true),
                SourceGroupId = table.Column<string>(maxLength: 450, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_UserTaskSnapshotMembers", x => x.Id));

        migrationBuilder.CreateTable(name: "UserTaskExclusions", schema: schema,
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 450, nullable: false),
                TenantId = table.Column<string>(maxLength: 450, nullable: false),
                TaskId = table.Column<string>(maxLength: 450, nullable: false),
                Provider = table.Column<string>(maxLength: 200, nullable: false),
                ParticipantKey = table.Column<string>(maxLength: 800, nullable: false),
                ParticipantType = table.Column<string>(maxLength: 32, nullable: false),
                ParticipantId = table.Column<string>(maxLength: 450, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_UserTaskExclusions", x => x.Id));

        migrationBuilder.CreateTable(name: "UserTaskEvents", schema: schema,
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 450, nullable: false),
                TenantId = table.Column<string>(maxLength: 450, nullable: false),
                TaskId = table.Column<string>(maxLength: 450, nullable: false),
                Revision = table.Column<int>(nullable: false),
                EventType = table.Column<string>(maxLength: 64, nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(nullable: false),
                OperationId = table.Column<string>(maxLength: 450, nullable: true),
                ActorProvider = table.Column<string>(maxLength: 200, nullable: true),
                ActorType = table.Column<string>(maxLength: 64, nullable: true),
                ActorId = table.Column<string>(maxLength: 450, nullable: true),
                ActorJson = table.Column<string>(nullable: true),
                Reason = table.Column<string>(maxLength: 4000, nullable: true),
                MetadataJson = table.Column<string>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_UserTaskEvents", x => x.Id));

        migrationBuilder.CreateTable(name: "UserTaskOperations", schema: schema,
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 450, nullable: false),
                TenantId = table.Column<string>(maxLength: 450, nullable: false),
                TaskId = table.Column<string>(maxLength: 450, nullable: false),
                OperationId = table.Column<string>(maxLength: 450, nullable: false),
                Kind = table.Column<string>(maxLength: 64, nullable: false),
                ExpectedRevision = table.Column<int>(nullable: false),
                RequestHash = table.Column<string>(maxLength: 128, nullable: false),
                Status = table.Column<string>(maxLength: 32, nullable: false),
                Attempts = table.Column<int>(nullable: false),
                SafeResultJson = table.Column<string>(nullable: true),
                ProtectedPayloadJson = table.Column<string>(nullable: true),
                ActionKey = table.Column<string>(maxLength: 200, nullable: true),
                ErrorCode = table.Column<string>(maxLength: 200, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_UserTaskOperations", x => x.Id));

        migrationBuilder.CreateTable(name: "UserTaskInvitations", schema: schema,
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 450, nullable: false),
                TenantId = table.Column<string>(maxLength: 450, nullable: false),
                TaskId = table.Column<string>(maxLength: 450, nullable: false),
                SiblingGroupId = table.Column<string>(maxLength: 450, nullable: true),
                RecipientJson = table.Column<string>(nullable: true),
                TokenHash = table.Column<string>(maxLength: 256, nullable: false),
                VerifierProvider = table.Column<string>(maxLength: 200, nullable: false),
                AllowedActionsJson = table.Column<string>(nullable: false),
                ChallengeJson = table.Column<string>(nullable: true),
                Status = table.Column<string>(maxLength: 32, nullable: false),
                IssuedAt = table.Column<DateTimeOffset>(nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(nullable: false),
                VerifiedAt = table.Column<DateTimeOffset>(nullable: true),
                ConsumedAt = table.Column<DateTimeOffset>(nullable: true),
                RevokedAt = table.Column<DateTimeOffset>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_UserTaskInvitations", x => x.Id));

        migrationBuilder.CreateTable(name: "UserTaskInvitationDeliveries", schema: schema,
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 450, nullable: false),
                TenantId = table.Column<string>(maxLength: 450, nullable: false),
                TaskId = table.Column<string>(maxLength: 450, nullable: false),
                InvitationId = table.Column<string>(maxLength: 450, nullable: false),
                DispatcherProvider = table.Column<string>(maxLength: 200, nullable: false),
                EncryptedToken = table.Column<string>(nullable: false),
                DeliveryMetadataJson = table.Column<string>(nullable: true),
                Status = table.Column<string>(maxLength: 32, nullable: false),
                Attempts = table.Column<int>(nullable: false),
                AvailableAt = table.Column<DateTimeOffset>(nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(nullable: false),
                LastErrorCode = table.Column<string>(maxLength: 128, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                DeliveredAt = table.Column<DateTimeOffset>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_UserTaskInvitationDeliveries", x => x.Id));

        migrationBuilder.CreateTable(name: "UserTaskGuestSessions", schema: schema,
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 450, nullable: false),
                TenantId = table.Column<string>(maxLength: 450, nullable: false),
                TaskId = table.Column<string>(maxLength: 450, nullable: false),
                InvitationId = table.Column<string>(maxLength: 450, nullable: false),
                SessionTokenHash = table.Column<string>(maxLength: 256, nullable: false),
                GuestParticipantJson = table.Column<string>(nullable: false),
                CapabilitiesJson = table.Column<string>(nullable: false),
                IssuedAt = table.Column<DateTimeOffset>(nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(nullable: false),
                RevokedAt = table.Column<DateTimeOffset>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_UserTaskGuestSessions", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_UserTasks_Tenant_MaterializationKey", table: "UserTasks", columns: ["TenantId", "MaterializationKey"], schema: schema, unique: true);
        migrationBuilder.CreateIndex(name: "IX_UserTasks_Tenant_BookmarkId", table: "UserTasks", columns: ["TenantId", "BookmarkId"], schema: schema, unique: true);
        migrationBuilder.CreateIndex(name: "IX_UserTasks_Tenant_Status", table: "UserTasks", columns: ["TenantId", "Status"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTasks_Tenant_Assignee", table: "UserTasks", columns: ["TenantId", "AssigneeProvider", "AssigneeType", "AssigneeId"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTasks_Tenant_Priority", table: "UserTasks", columns: ["TenantId", "Priority"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTasks_Tenant_DueAt", table: "UserTasks", columns: ["TenantId", "DueAt"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTasks_Tenant_WorkflowDefinition", table: "UserTasks", columns: ["TenantId", "WorkflowDefinitionId"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTasks_Tenant_WorkflowInstance", table: "UserTasks", columns: ["TenantId", "WorkflowInstanceId"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTasks_Tenant_ActivityInstance", table: "UserTasks", columns: ["TenantId", "ActivityInstanceId"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTasks_Tenant_CreatedAt", table: "UserTasks", columns: ["TenantId", "CreatedAt"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTasks_Tenant_CompletedAt", table: "UserTasks", columns: ["TenantId", "CompletedAt"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTaskCandidates_Tenant_Task_Participant", table: "UserTaskCandidates", columns: ["TenantId", "TaskId", "Provider", "ParticipantType", "ParticipantId", "Source"], schema: schema, unique: true);
        migrationBuilder.CreateIndex(name: "IX_UserTaskCandidates_Tenant_Participant", table: "UserTaskCandidates", columns: ["TenantId", "Provider", "ParticipantType", "ParticipantId"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTaskCandidates_Tenant_Task", table: "UserTaskCandidates", columns: ["TenantId", "TaskId"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTaskCandidates_Tenant_Task_ParticipantKey", table: "UserTaskCandidates", columns: ["TenantId", "TaskId", "ParticipantKey"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTaskSnapshotMembers_Tenant_Task_Participant", table: "UserTaskSnapshotMembers", columns: ["TenantId", "TaskId", "Provider", "ParticipantType", "ParticipantId"], schema: schema, unique: true);
        migrationBuilder.CreateIndex(name: "IX_UserTaskSnapshotMembers_Tenant_Task_ParticipantKey", table: "UserTaskSnapshotMembers", columns: ["TenantId", "TaskId", "ParticipantKey"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTaskExclusions_Tenant_Task_Participant", table: "UserTaskExclusions", columns: ["TenantId", "TaskId", "Provider", "ParticipantType", "ParticipantId"], schema: schema, unique: true);
        migrationBuilder.CreateIndex(name: "IX_UserTaskExclusions_Tenant_Task_ParticipantKey", table: "UserTaskExclusions", columns: ["TenantId", "TaskId", "ParticipantKey"], schema: schema);
        // Not unique: audit is append-only and several entries may share a revision, because an audited read
        // (a masked-field reveal) records without consuming the aggregate's concurrency token.
        migrationBuilder.CreateIndex(name: "IX_UserTaskEvents_Task_Revision", table: "UserTaskEvents", columns: ["TaskId", "Revision"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTaskEvents_Tenant_Task_OccurredAt", table: "UserTaskEvents", columns: ["TenantId", "TaskId", "OccurredAt"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTaskOperations_Tenant_Task_Operation", table: "UserTaskOperations", columns: ["TenantId", "TaskId", "OperationId"], schema: schema, unique: true);
        migrationBuilder.CreateIndex(name: "IX_UserTaskOperations_Tenant_Status_UpdatedAt", table: "UserTaskOperations", columns: ["TenantId", "Status", "UpdatedAt"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTaskInvitations_TokenHash", table: "UserTaskInvitations", column: "TokenHash", schema: schema, unique: true);
        migrationBuilder.CreateIndex(name: "IX_UserTaskInvitations_Tenant_Task_Status", table: "UserTaskInvitations", columns: ["TenantId", "TaskId", "Status"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTaskInvitations_Tenant_Status_ExpiresAt", table: "UserTaskInvitations", columns: ["TenantId", "Status", "ExpiresAt"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTaskInvitationDeliveries_Tenant_Status_AvailableAt", table: "UserTaskInvitationDeliveries", columns: ["TenantId", "Status", "AvailableAt"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTaskInvitationDeliveries_Tenant_Invitation", table: "UserTaskInvitationDeliveries", columns: ["TenantId", "InvitationId"], schema: schema, unique: true);
        migrationBuilder.CreateIndex(name: "IX_UserTaskGuestSessions_SessionTokenHash", table: "UserTaskGuestSessions", column: "SessionTokenHash", schema: schema, unique: true);
        migrationBuilder.CreateIndex(name: "IX_UserTaskGuestSessions_Tenant_Task_ExpiresAt", table: "UserTaskGuestSessions", columns: ["TenantId", "TaskId", "ExpiresAt"], schema: schema);
        migrationBuilder.CreateIndex(name: "IX_UserTaskGuestSessions_Tenant_Invitation", table: "UserTaskGuestSessions", columns: ["TenantId", "InvitationId"], schema: schema);
    }

    public static void Down(MigrationBuilder migrationBuilder, string schema)
    {
        migrationBuilder.DropTable("UserTaskGuestSessions", schema);
        migrationBuilder.DropTable("UserTaskInvitationDeliveries", schema);
        migrationBuilder.DropTable("UserTaskInvitations", schema);
        migrationBuilder.DropTable("UserTaskOperations", schema);
        migrationBuilder.DropTable("UserTaskEvents", schema);
        migrationBuilder.DropTable("UserTaskExclusions", schema);
        migrationBuilder.DropTable("UserTaskSnapshotMembers", schema);
        migrationBuilder.DropTable("UserTaskCandidates", schema);
        migrationBuilder.DropTable("UserTasks", schema);
    }
}
