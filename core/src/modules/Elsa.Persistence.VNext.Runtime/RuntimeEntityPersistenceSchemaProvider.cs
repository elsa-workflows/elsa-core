using Elsa.Persistence.VNext.Builders;
using Elsa.Persistence.VNext.Contracts;
using Elsa.Persistence.VNext.Runtime.Models;

namespace Elsa.Persistence.VNext.Runtime;

public class RuntimeEntityPersistenceSchemaProvider : IPersistenceSchemaProvider
{
    public const string DefinitionsStorageUnit = "RuntimeEntityDefinitions";
    public const string InstancesStorageUnit = "RuntimeEntityInstances";
    public const string AuditStorageUnit = "RuntimeEntityAudit";
    public const int IndexedFieldSlotCount = 4;

    public PersistenceSchema DescribeSchema()
    {
        var builder = new PersistenceSchemaBuilder("Elsa.RuntimeEntities").Version(1)
            .StorageUnit(DefinitionsStorageUnit, storage => storage
                .RequiredField(nameof(RuntimeEntityDefinition.Id), PersistenceColumnType.String, 450)
                .RequiredField(nameof(RuntimeEntityDefinition.Name), PersistenceColumnType.String, 200)
                .RequiredField(nameof(RuntimeEntityDefinition.Version), PersistenceColumnType.Int64)
                .RequiredField(nameof(RuntimeEntityDefinition.Status), PersistenceColumnType.String, 32)
                .RequiredField(nameof(RuntimeEntityDefinition.Fields), PersistenceColumnType.Json)
                .RequiredField(nameof(RuntimeEntityDefinition.Indexes), PersistenceColumnType.Json)
                .RequiredField(nameof(RuntimeEntityDefinition.CreatedAt), PersistenceColumnType.DateTimeOffset)
                .Field(nameof(RuntimeEntityDefinition.UpdatedAt), PersistenceColumnType.DateTimeOffset)
                .Key("PK_RuntimeEntityDefinitions", nameof(RuntimeEntityDefinition.Id))
                .Index("IX_RuntimeEntityDefinitions_Name", nameof(RuntimeEntityDefinition.Name), unique: true)
                .Index("IX_RuntimeEntityDefinitions_Status", nameof(RuntimeEntityDefinition.Status)),
                @namespace: "Elsa")
            .StorageUnit(InstancesStorageUnit, storage =>
            {
                storage
                    .RequiredField(nameof(RuntimeEntityInstance.Id), PersistenceColumnType.String, 450)
                    .RequiredField(nameof(RuntimeEntityInstance.DefinitionName), PersistenceColumnType.String, 200)
                    .RequiredField(nameof(RuntimeEntityInstance.DefinitionVersion), PersistenceColumnType.Int64)
                    .RequiredField(nameof(RuntimeEntityInstance.Data), PersistenceColumnType.Json)
                    .RequiredField(nameof(RuntimeEntityInstance.CreatedAt), PersistenceColumnType.DateTimeOffset)
                    .Field(nameof(RuntimeEntityInstance.UpdatedAt), PersistenceColumnType.DateTimeOffset)
                    .Key("PK_RuntimeEntityInstances", nameof(RuntimeEntityInstance.Id))
                    .Index("IX_RuntimeEntityInstances_DefinitionName", nameof(RuntimeEntityInstance.DefinitionName));

                for (var slot = 1; slot <= IndexedFieldSlotCount; slot++)
                {
                    storage
                        .RequiredField($"Index{slot}Name", PersistenceColumnType.String, 200)
                        .Field($"Index{slot}Value", PersistenceColumnType.String, length: 450)
                        .Index($"IX_RuntimeEntityInstances_Index{slot}", [nameof(RuntimeEntityInstance.DefinitionName), $"Index{slot}Name", $"Index{slot}Value"]);
                }
            }, @namespace: "Elsa")
            .StorageUnit(AuditStorageUnit, storage => storage
                .RequiredField(nameof(RuntimeEntityAuditRecord.Id), PersistenceColumnType.String, 450)
                .RequiredField(nameof(RuntimeEntityAuditRecord.SubjectType), PersistenceColumnType.String, 100)
                .RequiredField(nameof(RuntimeEntityAuditRecord.SubjectId), PersistenceColumnType.String, 450)
                .RequiredField(nameof(RuntimeEntityAuditRecord.Action), PersistenceColumnType.String, 100)
                .RequiredField(nameof(RuntimeEntityAuditRecord.Timestamp), PersistenceColumnType.DateTimeOffset)
                .Field(nameof(RuntimeEntityAuditRecord.Message), PersistenceColumnType.Text)
                .Key("PK_RuntimeEntityAudit", nameof(RuntimeEntityAuditRecord.Id))
                .Index("IX_RuntimeEntityAudit_SubjectId", nameof(RuntimeEntityAuditRecord.SubjectId))
                .Index("IX_RuntimeEntityAudit_Subject", [nameof(RuntimeEntityAuditRecord.SubjectType), nameof(RuntimeEntityAuditRecord.SubjectId)]),
                @namespace: "Elsa");

        return builder.Build();
    }
}
