using Elsa.Persistence.VNext;
using Elsa.Persistence.VNext.Builders;
using Elsa.Persistence.VNext.Contracts;
using Elsa.Secrets.Models;

namespace Elsa.Secrets.Persistence.VNext;

public class SecretPersistenceSchemaProvider : IPersistenceSchemaProvider
{
    public PersistenceSchema DescribeSchema()
    {
        return new PersistenceSchemaBuilder("Elsa.Secrets")
            .Version(1)
            .StorageUnit("Secrets", storage => storage
                .RequiredField(nameof(Secret.Id), PersistenceColumnType.String, 450)
                .RequiredField(nameof(Secret.Name), PersistenceColumnType.String, 200)
                .RequiredField(nameof(Secret.DisplayName), PersistenceColumnType.String, 200)
                .Field(nameof(Secret.Description), PersistenceColumnType.Text)
                .RequiredField(nameof(Secret.TypeName), PersistenceColumnType.String, 100)
                .RequiredField(nameof(Secret.StoreName), PersistenceColumnType.String, 100)
                .Field(nameof(Secret.Scope), PersistenceColumnType.String, length: 200)
                .RequiredField(nameof(Secret.Status), PersistenceColumnType.String, 32)
                .RequiredField(nameof(Secret.CreatedAt), PersistenceColumnType.DateTimeOffset)
                .Field(nameof(Secret.UpdatedAt), PersistenceColumnType.DateTimeOffset)
                .RequiredField(nameof(Secret.Tags), PersistenceColumnType.Json)
                .RequiredField(nameof(Secret.Versions), PersistenceColumnType.Json)
                .Key("PK_Secrets", nameof(Secret.Id))
                .Index("IX_Secret_Name", nameof(Secret.Name), unique: true)
                .Index("IX_Secret_TypeName", nameof(Secret.TypeName))
                .Index("IX_Secret_StoreName", nameof(Secret.StoreName))
                .Index("IX_Secret_Scope", nameof(Secret.Scope))
                .Index("IX_Secret_Status", nameof(Secret.Status)),
                @namespace: "Elsa")
            .Build();
    }
}
