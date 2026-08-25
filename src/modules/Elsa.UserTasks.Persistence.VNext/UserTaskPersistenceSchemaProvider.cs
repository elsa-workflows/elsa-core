using Elsa.Persistence.VNext;
using Elsa.Persistence.VNext.Builders;
using Elsa.Persistence.VNext.Contracts;

namespace Elsa.UserTasks.Persistence.VNext;

public sealed class UserTaskPersistenceSchemaProvider : IPersistenceSchemaProvider
{
    public PersistenceSchema DescribeSchema()
    {
        var schema = new PersistenceSchemaBuilder("Elsa.UserTasks").Version(1);
        schema.StorageUnit("UserTasks", storage => storage
            .RequiredField("Id", PersistenceColumnType.String, 450).RequiredField("TenantId", PersistenceColumnType.String, 450)
            .RequiredField("WorkflowDefinitionId", PersistenceColumnType.String, 450).RequiredField("WorkflowInstanceId", PersistenceColumnType.String, 450)
            .RequiredField("ActivityInstanceId", PersistenceColumnType.String, 450).RequiredField("BookmarkId", PersistenceColumnType.String, 450)
            .RequiredField("MaterializationKey", PersistenceColumnType.String, 900).RequiredField("Title", PersistenceColumnType.String, 500)
            .Field("Summary", PersistenceColumnType.Text).Field("Reference", PersistenceColumnType.String, length: 500).Field("TaskType", PersistenceColumnType.String, length: 200)
            .RequiredField("TagsJson", PersistenceColumnType.Json).Field("Requester", PersistenceColumnType.Json).Field("Assignee", PersistenceColumnType.Json)
            .Field("RequesterProvider", PersistenceColumnType.String, length: 200).Field("RequesterType", PersistenceColumnType.String, length: 64)
            .Field("RequesterId", PersistenceColumnType.String, length: 450).Field("RequesterDisplayName", PersistenceColumnType.String, length: 500)
            .Field("AssigneeProvider", PersistenceColumnType.String, length: 200).Field("AssigneeType", PersistenceColumnType.String, length: 32).Field("AssigneeId", PersistenceColumnType.String, length: 450)
            .Field("HealthSeverity", PersistenceColumnType.String, length: 32).Field("HealthCode", PersistenceColumnType.String, length: 200).Field("HealthMessage", PersistenceColumnType.Text)
            .RequiredField("Status", PersistenceColumnType.String, 32).RequiredField("Priority", PersistenceColumnType.Int32).Field("DueAt", PersistenceColumnType.DateTimeOffset)
            .RequiredField("IsOverdue", PersistenceColumnType.Boolean).RequiredField("TimeoutEnabled", PersistenceColumnType.Boolean)
            .RequiredField("CancellationEnabled", PersistenceColumnType.Boolean).RequiredField("AllowManagerExclusionOverride", PersistenceColumnType.Boolean)
            .Field("MembershipResolutionMode", PersistenceColumnType.String, length: 32).RequiredField("Revision", PersistenceColumnType.Int32)
            .RequiredField("InstructionsJson", PersistenceColumnType.Json).RequiredField("TaskDataJson", PersistenceColumnType.Json)
            .Field("FormReferenceJson", PersistenceColumnType.Json).Field("PinnedFormJson", PersistenceColumnType.Json).Field("ActionsJson", PersistenceColumnType.Json)
            .RequiredField("InvitationDefinitionsJson", PersistenceColumnType.Json).RequiredField("CompletionDataJson", PersistenceColumnType.Json)
            .RequiredField("HealthIssuesJson", PersistenceColumnType.Json).Field("CompletionActionKey", PersistenceColumnType.String, length: 200).Field("CompletionActorJson", PersistenceColumnType.Json)
            .RequiredField("CreatedAt", PersistenceColumnType.DateTimeOffset).RequiredField("UpdatedAt", PersistenceColumnType.DateTimeOffset)
            .Field("AssignedAt", PersistenceColumnType.DateTimeOffset).Field("CompletedAt", PersistenceColumnType.DateTimeOffset).Field("CreatedFromBookmarkRevision", PersistenceColumnType.Int64)
            .Key("PK_UserTasks", "Id").Index("IX_UserTasks_Tenant_MaterializationKey", ["TenantId", "MaterializationKey"], unique: true)
            .Index("IX_UserTasks_Tenant_BookmarkId", ["TenantId", "BookmarkId"], unique: true).Index("IX_UserTasks_Tenant_Status", ["TenantId", "Status"])
            .Index("IX_UserTasks_Tenant_Assignee", ["TenantId", "AssigneeProvider", "AssigneeType", "AssigneeId"]).Index("IX_UserTasks_Tenant_Priority", ["TenantId", "Priority"])
            .Index("IX_UserTasks_Tenant_DueAt", ["TenantId", "DueAt"]).Index("IX_UserTasks_Tenant_WorkflowDefinition", ["TenantId", "WorkflowDefinitionId"])
            .Index("IX_UserTasks_Tenant_WorkflowInstance", ["TenantId", "WorkflowInstanceId"]).Index("IX_UserTasks_Tenant_ActivityInstance", ["TenantId", "ActivityInstanceId"])
            .Index("IX_UserTasks_Tenant_CreatedAt", ["TenantId", "CreatedAt"]).Index("IX_UserTasks_Tenant_CompletedAt", ["TenantId", "CompletedAt"]), @namespace: "Elsa.UserTasks");
        AddParticipantUnit(schema, "UserTaskCandidates", includeSource: true);
        AddParticipantUnit(schema, "UserTaskSnapshotMembers", includeSource: false);
        AddParticipantUnit(schema, "UserTaskExclusions", includeSource: false);
        schema.StorageUnit("UserTaskEvents", storage => storage
            .RequiredField("Id", PersistenceColumnType.String, 450).RequiredField("TenantId", PersistenceColumnType.String, 450).RequiredField("TaskId", PersistenceColumnType.String, 450)
            .RequiredField("Revision", PersistenceColumnType.Int32).RequiredField("EventType", PersistenceColumnType.String, 64).RequiredField("OccurredAt", PersistenceColumnType.DateTimeOffset)
            .Field("OperationId", PersistenceColumnType.String, length: 450).Field("ActorProvider", PersistenceColumnType.String, length: 200).Field("ActorType", PersistenceColumnType.String, length: 64)
            .Field("ActorId", PersistenceColumnType.String, length: 450).Field("Actor", PersistenceColumnType.Json).Field("Reason", PersistenceColumnType.Text).RequiredField("MetadataJson", PersistenceColumnType.Json)
            .Key("PK_UserTaskEvents", "Id").Index("IX_UserTaskEvents_Task_Revision", ["TaskId", "Revision"], unique: true).Index("IX_UserTaskEvents_Tenant_Task_OccurredAt", ["TenantId", "TaskId", "OccurredAt"]), @namespace: "Elsa.UserTasks");
        schema.StorageUnit("UserTaskOperations", storage => storage
            .RequiredField("Id", PersistenceColumnType.String, 450).RequiredField("TenantId", PersistenceColumnType.String, 450).RequiredField("TaskId", PersistenceColumnType.String, 450)
            .RequiredField("OperationId", PersistenceColumnType.String, 450).RequiredField("Kind", PersistenceColumnType.String, 64).RequiredField("ExpectedRevision", PersistenceColumnType.Int32)
            .RequiredField("RequestHash", PersistenceColumnType.String, 128).RequiredField("Status", PersistenceColumnType.String, 32).Field("ActionKey", PersistenceColumnType.String, length: 200)
            .Field("ErrorCode", PersistenceColumnType.String, length: 200).RequiredField("Attempts", PersistenceColumnType.Int32).Field("SafeResultJson", PersistenceColumnType.Json)
            .Field("ProtectedPayloadJson", PersistenceColumnType.Json).RequiredField("CreatedAt", PersistenceColumnType.DateTimeOffset).RequiredField("UpdatedAt", PersistenceColumnType.DateTimeOffset).Field("CompletedAt", PersistenceColumnType.DateTimeOffset)
            .Key("PK_UserTaskOperations", "Id").Index("IX_UserTaskOperations_Tenant_Task_Operation", ["TenantId", "TaskId", "OperationId"], unique: true), @namespace: "Elsa.UserTasks");
        schema.StorageUnit("UserTaskInvitations", storage => storage
            .RequiredField("Id", PersistenceColumnType.String, 450).RequiredField("TenantId", PersistenceColumnType.String, 450).RequiredField("TaskId", PersistenceColumnType.String, 450)
            .Field("SiblingGroupId", PersistenceColumnType.String, length: 450).Field("RecipientJson", PersistenceColumnType.Json).RequiredField("TokenHash", PersistenceColumnType.String, 256).RequiredField("VerifierProvider", PersistenceColumnType.String, 200)
            .Field("ChallengeJson", PersistenceColumnType.Json)
            .RequiredField("Status", PersistenceColumnType.String, 32).RequiredField("IssuedAt", PersistenceColumnType.DateTimeOffset).RequiredField("ExpiresAt", PersistenceColumnType.DateTimeOffset)
            .Field("VerifiedAt", PersistenceColumnType.DateTimeOffset).Field("ConsumedAt", PersistenceColumnType.DateTimeOffset).Field("RevokedAt", PersistenceColumnType.DateTimeOffset)
            .Key("PK_UserTaskInvitations", "Id").Index("IX_UserTaskInvitations_TokenHash", "TokenHash", unique: true).Index("IX_UserTaskInvitations_Tenant_Task_Status", ["TenantId", "TaskId", "Status"]), @namespace: "Elsa.UserTasks");
        schema.StorageUnit("UserTaskInvitationDeliveries", storage => storage
            .RequiredField("Id", PersistenceColumnType.String, 450).RequiredField("TenantId", PersistenceColumnType.String, 450).RequiredField("TaskId", PersistenceColumnType.String, 450)
            .RequiredField("InvitationId", PersistenceColumnType.String, 450).RequiredField("DispatcherProvider", PersistenceColumnType.String, 200).RequiredField("EncryptedToken", PersistenceColumnType.Text)
            .Field("DeliveryMetadataJson", PersistenceColumnType.Json).RequiredField("Status", PersistenceColumnType.String, 32).RequiredField("Attempts", PersistenceColumnType.Int32)
            .RequiredField("AvailableAt", PersistenceColumnType.DateTimeOffset).RequiredField("ExpiresAt", PersistenceColumnType.DateTimeOffset).Field("LastErrorCode", PersistenceColumnType.String, length: 128)
            .RequiredField("CreatedAt", PersistenceColumnType.DateTimeOffset).Field("DeliveredAt", PersistenceColumnType.DateTimeOffset)
            .Key("PK_UserTaskInvitationDeliveries", "Id").Index("IX_UserTaskInvitationDeliveries_Tenant_Status_AvailableAt", ["TenantId", "Status", "AvailableAt"])
            .Index("IX_UserTaskInvitationDeliveries_Tenant_Invitation", ["TenantId", "InvitationId"], unique: true), @namespace: "Elsa.UserTasks");
        schema.StorageUnit("UserTaskGuestSessions", storage => storage
            .RequiredField("Id", PersistenceColumnType.String, 450).RequiredField("TenantId", PersistenceColumnType.String, 450).RequiredField("TaskId", PersistenceColumnType.String, 450)
            .RequiredField("InvitationId", PersistenceColumnType.String, 450).RequiredField("SessionTokenHash", PersistenceColumnType.String, 256).RequiredField("GuestParticipantJson", PersistenceColumnType.Json)
            .RequiredField("CapabilitiesJson", PersistenceColumnType.Json).RequiredField("IssuedAt", PersistenceColumnType.DateTimeOffset).RequiredField("ExpiresAt", PersistenceColumnType.DateTimeOffset).Field("RevokedAt", PersistenceColumnType.DateTimeOffset)
            .Key("PK_UserTaskGuestSessions", "Id").Index("IX_UserTaskGuestSessions_SessionTokenHash", "SessionTokenHash", unique: true).Index("IX_UserTaskGuestSessions_Tenant_Task_ExpiresAt", ["TenantId", "TaskId", "ExpiresAt"]).Index("IX_UserTaskGuestSessions_Tenant_Invitation", ["TenantId", "InvitationId"]), @namespace: "Elsa.UserTasks");
        return schema.Build();
    }

    private static void AddParticipantUnit(PersistenceSchemaBuilder schema, string name, bool includeSource) => schema.StorageUnit(name, storage =>
    {
        storage.RequiredField("Id", PersistenceColumnType.String, 450).RequiredField("TenantId", PersistenceColumnType.String, 450).RequiredField("TaskId", PersistenceColumnType.String, 450)
            .RequiredField("Provider", PersistenceColumnType.String, 200).RequiredField("ParticipantKey", PersistenceColumnType.String, 800).RequiredField("ParticipantType", PersistenceColumnType.String, 32)
            .RequiredField("ParticipantId", PersistenceColumnType.String, 450).Field("DisplayName", PersistenceColumnType.String, length: 500);
        if (includeSource)
            storage.RequiredField("Source", PersistenceColumnType.String, 32).Field("SourceGroupProvider", PersistenceColumnType.String, length: 200).Field("SourceGroupId", PersistenceColumnType.String, length: 450);
        storage.RequiredField("CreatedAt", PersistenceColumnType.DateTimeOffset).Key($"PK_{name}", "Id");
        if (includeSource)
            storage.Index($"IX_{name}_Tenant_Task_Participant", ["TenantId", "TaskId", "Provider", "ParticipantType", "ParticipantId", "Source"], unique: true);
        else
            storage.Index($"IX_{name}_Tenant_Task_Participant", ["TenantId", "TaskId", "Provider", "ParticipantType", "ParticipantId"], unique: true);
        storage.Index($"IX_{name}_Tenant_Participant", ["TenantId", "Provider", "ParticipantType", "ParticipantId"])
            .Index($"IX_{name}_Tenant_Task_ParticipantKey", ["TenantId", "TaskId", "ParticipantKey"]);
    }, @namespace: "Elsa.UserTasks");
}
