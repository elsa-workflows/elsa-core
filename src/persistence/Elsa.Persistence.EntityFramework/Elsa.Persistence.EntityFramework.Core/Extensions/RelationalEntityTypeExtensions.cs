using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.Persistence.EntityFramework.Core.Extensions
{
    public static class RelationalEntityTypeExtensions
    {
        private static string AddQuotes(string src)
        {
            if (!src.StartsWith("\""))
            {
                src = $"\"{src}\"";
            }
            return src;
        }
        public static string? GetSchemaQualifiedTableNameWithQuotes([NotNull] this IEntityType entityType)
        {
            var tableName = entityType.GetTableName();
            if (tableName == null)
            {
                return null;
            }
            var schema = entityType.GetSchema();
            if (string.IsNullOrEmpty(schema))
            {
                return AddQuotes(tableName);
            }
            else
            {
                return $"{AddQuotes(schema)}.{AddQuotes(tableName)}";
            }
        }
    }
}