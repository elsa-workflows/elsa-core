using Elsa.Persistence.VNext.Document;

namespace Elsa.Persistence.VNext.Relational.Documents;

public abstract class RelationalDocumentStoreDialect
{
    public string DocumentsTable => QuoteIdentifier("ElsaDocuments");
    public string IndexValuesTable => QuoteIdentifier("ElsaDocumentIndexValues");

    public abstract string Parameter(string name);

    public abstract string QuoteIdentifier(string identifier);

    public virtual IReadOnlyList<string> CreateMaterializationLockStatements()
    {
        return [];
    }

    public virtual IReadOnlyList<string> CreateMaterializationStatements()
    {
        return [
            $"""
            CREATE TABLE IF NOT EXISTS {DocumentsTable} (
                {QuoteIdentifier("StorageUnit")} TEXT NOT NULL,
                {QuoteIdentifier("Id")} TEXT NOT NULL,
                {QuoteIdentifier("Content")} TEXT NOT NULL,
                {QuoteIdentifier("Version")} BIGINT NOT NULL,
                {QuoteIdentifier("CreatedAt")} TEXT NOT NULL,
                {QuoteIdentifier("UpdatedAt")} TEXT NOT NULL,
                CONSTRAINT {QuoteIdentifier("PK_ElsaDocuments")} PRIMARY KEY ({QuoteIdentifier("StorageUnit")}, {QuoteIdentifier("Id")})
            );
            """,
            $"""
            CREATE TABLE IF NOT EXISTS {IndexValuesTable} (
                {QuoteIdentifier("StorageUnit")} TEXT NOT NULL,
                {QuoteIdentifier("IndexName")} TEXT NOT NULL,
                {QuoteIdentifier("FieldName")} TEXT NOT NULL,
                {QuoteIdentifier("FieldValue")} TEXT,
                {QuoteIdentifier("DocumentId")} TEXT NOT NULL
            );
            """,
            $"""
            CREATE INDEX IF NOT EXISTS {QuoteIdentifier("IX_ElsaDocumentIndexValues_Lookup")}
            ON {IndexValuesTable} ({QuoteIdentifier("StorageUnit")}, {QuoteIdentifier("IndexName")}, {QuoteIdentifier("FieldName")}, {QuoteIdentifier("FieldValue")}, {QuoteIdentifier("DocumentId")});
            """
        ];
    }

    public virtual string RenderSelectDocumentSql()
    {
        return $"""
               SELECT {QuoteIdentifier("StorageUnit")}, {QuoteIdentifier("Id")}, {QuoteIdentifier("Content")}, {QuoteIdentifier("Version")}, {QuoteIdentifier("CreatedAt")}, {QuoteIdentifier("UpdatedAt")}
               FROM {DocumentsTable}
               WHERE {QuoteIdentifier("StorageUnit")} = {Parameter("storageUnit")} AND {QuoteIdentifier("Id")} = {Parameter("id")};
               """;
    }

    public virtual string RenderUpsertDocumentSql()
    {
        return $"""
               INSERT INTO {DocumentsTable} ({QuoteIdentifier("StorageUnit")}, {QuoteIdentifier("Id")}, {QuoteIdentifier("Content")}, {QuoteIdentifier("Version")}, {QuoteIdentifier("CreatedAt")}, {QuoteIdentifier("UpdatedAt")})
               VALUES ({Parameter("storageUnit")}, {Parameter("id")}, {Parameter("content")}, {Parameter("version")}, {Parameter("createdAt")}, {Parameter("updatedAt")})
               ON CONFLICT({QuoteIdentifier("StorageUnit")}, {QuoteIdentifier("Id")}) DO UPDATE SET
                   {QuoteIdentifier("Content")} = excluded.{QuoteIdentifier("Content")},
                   {QuoteIdentifier("Version")} = excluded.{QuoteIdentifier("Version")},
                   {QuoteIdentifier("UpdatedAt")} = excluded.{QuoteIdentifier("UpdatedAt")};
               """;
    }

    public virtual string RenderDeleteIndexValuesSql()
    {
        return $"""DELETE FROM {IndexValuesTable} WHERE {QuoteIdentifier("StorageUnit")} = {Parameter("storageUnit")} AND {QuoteIdentifier("DocumentId")} = {Parameter("documentId")};""";
    }

    public virtual string RenderInsertIndexValueSql()
    {
        return $"""
               INSERT INTO {IndexValuesTable} ({QuoteIdentifier("StorageUnit")}, {QuoteIdentifier("IndexName")}, {QuoteIdentifier("FieldName")}, {QuoteIdentifier("FieldValue")}, {QuoteIdentifier("DocumentId")})
               VALUES ({Parameter("storageUnit")}, {Parameter("indexName")}, {Parameter("fieldName")}, {Parameter("fieldValue")}, {Parameter("documentId")});
               """;
    }

    public virtual string RenderDeleteDocumentSql()
    {
        return $"""DELETE FROM {DocumentsTable} WHERE {QuoteIdentifier("StorageUnit")} = {Parameter("storageUnit")} AND {QuoteIdentifier("Id")} = {Parameter("documentId")};""";
    }

    public virtual string RenderQueryDocumentIdsSql(IReadOnlyList<string> filters)
    {
        return $"""
               SELECT {QuoteIdentifier("DocumentId")}
               FROM {IndexValuesTable}
               WHERE {QuoteIdentifier("StorageUnit")} = {Parameter("storageUnit")}
                 AND {QuoteIdentifier("IndexName")} = {Parameter("indexName")}
                 AND ({string.Join(" OR ", filters)})
               GROUP BY {QuoteIdentifier("DocumentId")}
               HAVING COUNT(DISTINCT {QuoteIdentifier("FieldName")}) = {Parameter("fieldCount")}
               ORDER BY {QuoteIdentifier("DocumentId")};
               """;
    }

    public virtual string RenderFieldFilter(string fieldParameter, string valueParameter)
    {
        return $"""({QuoteIdentifier("FieldName")} = {fieldParameter} AND ({QuoteIdentifier("FieldValue")} = {valueParameter} OR ({QuoteIdentifier("FieldValue")} IS NULL AND {valueParameter} IS NULL)))""";
    }

    public virtual object? ConvertStoredValue(string? value)
    {
        return value;
    }

    public virtual object ConvertTimestamp(DateTimeOffset value)
    {
        return value.ToString("O");
    }

    public DocumentIndex FindMatchingIndex(DocumentCollection collection, DocumentQuery query)
    {
        return DocumentIndexMatcher.FindMatchingIndex(collection, query);
    }
}
