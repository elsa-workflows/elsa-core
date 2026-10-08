using Elsa.Persistence.VNext.Relational.Documents;

namespace Elsa.Persistence.VNext.SqlServer;

public class SqlServerDocumentStoreDialect : RelationalDocumentStoreDialect
{
    public override string Parameter(string name)
    {
        return $"@{name}";
    }

    public override string QuoteIdentifier(string identifier)
    {
        return $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
    }

    public override IReadOnlyList<string> CreateMaterializationLockStatements()
    {
        return [
            """
            EXEC sp_getapplock
                @Resource = N'Elsa.Persistence.VNext.Documents',
                @LockMode = N'Exclusive',
                @LockOwner = N'Transaction',
                @LockTimeout = 60000;
            """
        ];
    }

    public override IReadOnlyList<string> CreateMaterializationStatements()
    {
        return [
            $"""
            IF OBJECT_ID(N{QuoteStringLiteral("ElsaDocuments")}, N'U') IS NULL
            BEGIN
                CREATE TABLE {DocumentsTable} (
                    {QuoteIdentifier("StorageUnit")} nvarchar(450) NOT NULL,
                    {QuoteIdentifier("Id")} nvarchar(450) NOT NULL,
                    {QuoteIdentifier("Content")} nvarchar(max) NOT NULL,
                    {QuoteIdentifier("Version")} bigint NOT NULL,
                    {QuoteIdentifier("CreatedAt")} nvarchar(64) NOT NULL,
                    {QuoteIdentifier("UpdatedAt")} nvarchar(64) NOT NULL,
                    CONSTRAINT {QuoteIdentifier("PK_ElsaDocuments")} PRIMARY KEY ({QuoteIdentifier("StorageUnit")}, {QuoteIdentifier("Id")})
                );
            END;
            """,
            $"""
            IF OBJECT_ID(N{QuoteStringLiteral("ElsaDocumentIndexValues")}, N'U') IS NULL
            BEGIN
                CREATE TABLE {IndexValuesTable} (
                    {QuoteIdentifier("StorageUnit")} nvarchar(450) NOT NULL,
                    {QuoteIdentifier("IndexName")} nvarchar(450) NOT NULL,
                    {QuoteIdentifier("FieldName")} nvarchar(450) NOT NULL,
                    {QuoteIdentifier("FieldValue")} nvarchar(450) NULL,
                    {QuoteIdentifier("DocumentId")} nvarchar(450) NOT NULL
                );
            END;
            """,
            $"""
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ElsaDocumentIndexValues_Lookup' AND object_id = OBJECT_ID(N{QuoteStringLiteral("ElsaDocumentIndexValues")}))
                CREATE INDEX {QuoteIdentifier("IX_ElsaDocumentIndexValues_Lookup")}
                ON {IndexValuesTable} ({QuoteIdentifier("StorageUnit")}, {QuoteIdentifier("IndexName")}, {QuoteIdentifier("FieldName")}, {QuoteIdentifier("FieldValue")}, {QuoteIdentifier("DocumentId")});
            """
        ];
    }

    public override string RenderUpsertDocumentSql()
    {
        return $"""
               UPDATE {DocumentsTable}
               SET {QuoteIdentifier("Content")} = {Parameter("content")},
                   {QuoteIdentifier("Version")} = {Parameter("version")},
                   {QuoteIdentifier("UpdatedAt")} = {Parameter("updatedAt")}
               WHERE {QuoteIdentifier("StorageUnit")} = {Parameter("storageUnit")} AND {QuoteIdentifier("Id")} = {Parameter("id")};

               IF @@ROWCOUNT = 0
               BEGIN
                   INSERT INTO {DocumentsTable} ({QuoteIdentifier("StorageUnit")}, {QuoteIdentifier("Id")}, {QuoteIdentifier("Content")}, {QuoteIdentifier("Version")}, {QuoteIdentifier("CreatedAt")}, {QuoteIdentifier("UpdatedAt")})
                   VALUES ({Parameter("storageUnit")}, {Parameter("id")}, {Parameter("content")}, {Parameter("version")}, {Parameter("createdAt")}, {Parameter("updatedAt")});
               END;
               """;
    }

    private static string QuoteStringLiteral(string value)
    {
        return $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
    }
}
