using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Elsa.Persistence.EntityFramework.Core.Extensions
{
    public static class RelationalEntityTypeExtensions
    {
        public static string? GetSchemaQualifiedTableNameWithQuotes(this IEntityType entityType, ISqlGenerationHelper helper)
        {
            var tableName = entityType.GetTableName();
            if (tableName == null)
            {
                return null;
            }

            var schema = entityType.GetSchema();
            return string.IsNullOrEmpty(schema)
                ? helper.DelimitIdentifier(tableName)
                : $"{helper.DelimitIdentifier(schema)}.{helper.DelimitIdentifier(tableName)}";
        }
    }
}