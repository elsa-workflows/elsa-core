using Elsa.ModularPersistence.Descriptors;

namespace Elsa.Workflows.Management.Persistence.ModularPersistence.Storage;

public static class WorkflowInstanceMetadataStorageManifest
{
    public const string SchemaName = "elsa.workflow-instances";
    public const string StorageUnitName = "WorkflowInstanceMetadata";

    public const string IdIndexName = "IX_WorkflowInstanceMetadata_Id";
    public const string DefinitionIdIndexName = "IX_WorkflowInstanceMetadata_DefinitionId";
    public const string DefinitionVersionIdIndexName = "IX_WorkflowInstanceMetadata_DefinitionVersionId";
    public const string DefinitionIdVersionIndexName = "IX_WorkflowInstanceMetadata_DefinitionId_Version";
    public const string VersionIndexName = "IX_WorkflowInstanceMetadata_Version";
    public const string ParentWorkflowInstanceIdIndexName = "IX_WorkflowInstanceMetadata_ParentWorkflowInstanceId";
    public const string CorrelationIdIndexName = "IX_WorkflowInstanceMetadata_CorrelationId";
    public const string NameIndexName = "IX_WorkflowInstanceMetadata_Name";
    public const string StatusIndexName = "IX_WorkflowInstanceMetadata_Status";
    public const string SubStatusIndexName = "IX_WorkflowInstanceMetadata_SubStatus";
    public const string StatusSubStatusIndexName = "IX_WorkflowInstanceMetadata_Status_SubStatus";
    public const string StatusDefinitionIdIndexName = "IX_WorkflowInstanceMetadata_Status_DefinitionId";
    public const string SubStatusDefinitionIdIndexName = "IX_WorkflowInstanceMetadata_SubStatus_DefinitionId";
    public const string StatusSubStatusDefinitionVersionIndexName = "IX_WorkflowInstanceMetadata_Status_SubStatus_DefinitionId_Version";
    public const string IsExecutingIndexName = "IX_WorkflowInstanceMetadata_IsExecuting";
    public const string HasIncidentsIndexName = "IX_WorkflowInstanceMetadata_IncidentCount";
    public const string IsSystemIndexName = "IX_WorkflowInstanceMetadata_IsSystem";
    public const string CreatedAtIndexName = "IX_WorkflowInstanceMetadata_CreatedAt";
    public const string UpdatedAtIndexName = "IX_WorkflowInstanceMetadata_UpdatedAt";
    public const string FinishedAtIndexName = "IX_WorkflowInstanceMetadata_FinishedAt";

    public static StorageManifestDescriptor Create() =>
        new(
            SchemaName,
            new StorageManifestVersion(1),
            [
                new StorageUnitDescriptor(
                    StorageUnitName,
                    [
                        new StorageFieldDescriptor("Id", StorageFieldType.String, true),
                        new StorageFieldDescriptor("TenantId", StorageFieldType.String),
                        new StorageFieldDescriptor("DefinitionId", StorageFieldType.String, true),
                        new StorageFieldDescriptor("DefinitionVersionId", StorageFieldType.String, true),
                        new StorageFieldDescriptor("Version", StorageFieldType.Int32, true),
                        new StorageFieldDescriptor("ParentWorkflowInstanceId", StorageFieldType.String),
                        new StorageFieldDescriptor("Status", StorageFieldType.String, true),
                        new StorageFieldDescriptor("SubStatus", StorageFieldType.String, true),
                        new StorageFieldDescriptor("IsExecuting", StorageFieldType.Boolean, true),
                        new StorageFieldDescriptor("CorrelationId", StorageFieldType.String),
                        new StorageFieldDescriptor("Name", StorageFieldType.String),
                        new StorageFieldDescriptor("IncidentCount", StorageFieldType.Int32, true),
                        new StorageFieldDescriptor("IsSystem", StorageFieldType.Boolean, true),
                        new StorageFieldDescriptor("CreatedAt", StorageFieldType.DateTimeOffset, true),
                        new StorageFieldDescriptor("UpdatedAt", StorageFieldType.DateTimeOffset, true),
                        new StorageFieldDescriptor("FinishedAt", StorageFieldType.DateTimeOffset)
                    ],
                    [
                        new StorageKeyDescriptor("PK_WorkflowInstanceMetadata", ["Id"])
                    ],
                    [
                        new StorageIndexDescriptor(IdIndexName, [new StorageIndexFieldDescriptor("Id")], true),
                        new StorageIndexDescriptor(DefinitionIdIndexName, [new StorageIndexFieldDescriptor("DefinitionId")]),
                        new StorageIndexDescriptor(DefinitionVersionIdIndexName, [new StorageIndexFieldDescriptor("DefinitionVersionId")]),
                        new StorageIndexDescriptor(DefinitionIdVersionIndexName, [new StorageIndexFieldDescriptor("DefinitionId"), new StorageIndexFieldDescriptor("Version")]),
                        new StorageIndexDescriptor(VersionIndexName, [new StorageIndexFieldDescriptor("Version")]),
                        new StorageIndexDescriptor(ParentWorkflowInstanceIdIndexName, [new StorageIndexFieldDescriptor("ParentWorkflowInstanceId")]),
                        new StorageIndexDescriptor(CorrelationIdIndexName, [new StorageIndexFieldDescriptor("CorrelationId")]),
                        new StorageIndexDescriptor(NameIndexName, [new StorageIndexFieldDescriptor("Name")]),
                        new StorageIndexDescriptor(StatusIndexName, [new StorageIndexFieldDescriptor("Status")]),
                        new StorageIndexDescriptor(SubStatusIndexName, [new StorageIndexFieldDescriptor("SubStatus")]),
                        new StorageIndexDescriptor(StatusSubStatusIndexName, [new StorageIndexFieldDescriptor("Status"), new StorageIndexFieldDescriptor("SubStatus")]),
                        new StorageIndexDescriptor(StatusDefinitionIdIndexName, [new StorageIndexFieldDescriptor("Status"), new StorageIndexFieldDescriptor("DefinitionId")]),
                        new StorageIndexDescriptor(SubStatusDefinitionIdIndexName, [new StorageIndexFieldDescriptor("SubStatus"), new StorageIndexFieldDescriptor("DefinitionId")]),
                        new StorageIndexDescriptor(StatusSubStatusDefinitionVersionIndexName, [new StorageIndexFieldDescriptor("Status"), new StorageIndexFieldDescriptor("SubStatus"), new StorageIndexFieldDescriptor("DefinitionId"), new StorageIndexFieldDescriptor("Version")]),
                        new StorageIndexDescriptor(IsExecutingIndexName, [new StorageIndexFieldDescriptor("IsExecuting")]),
                        new StorageIndexDescriptor(HasIncidentsIndexName, [new StorageIndexFieldDescriptor("IncidentCount")]),
                        new StorageIndexDescriptor(IsSystemIndexName, [new StorageIndexFieldDescriptor("IsSystem")]),
                        new StorageIndexDescriptor(CreatedAtIndexName, [new StorageIndexFieldDescriptor("CreatedAt")]),
                        new StorageIndexDescriptor(UpdatedAtIndexName, [new StorageIndexFieldDescriptor("UpdatedAt")]),
                        new StorageIndexDescriptor(FinishedAtIndexName, [new StorageIndexFieldDescriptor("FinishedAt")])
                    ])
            ]);
}
