using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.Persistence.EFCore.PostgreSql.Helpers;

public static class TableNameHelpers
{
    public static string QuoteSchemaQualifiedTableName(this IEntityType entityType)
    {
        var schemaQualifiedTableName = entityType.GetSchemaQualifiedTableName()!;
        return QuoteSchemaQualifiedTableName(schemaQualifiedTableName);
    }
    
    public static string QuoteSchemaQualifiedTableName(string schemaQualifiedTableName)
    {
        var parts = schemaQualifiedTableName.Split('.');
        return string.Join(".", parts.Select(p => $"\"{p}\""));
    }
}