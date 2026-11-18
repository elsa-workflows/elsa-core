using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.Persistence.EFCore.Extensions;

internal static class MySqlStoreKeyVerifier
{
    internal static async Task<bool> VerifyAsync(DbContext context, StoreObjectIdentifier table, string column, int maximumIdLength, CancellationToken cancellationToken)
    {
        return await context.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS `Value`
            FROM information_schema.statistics k
            JOIN information_schema.columns c USING (table_schema, table_name, column_name)
            WHERE k.table_schema = DATABASE() AND k.table_name = {table.Name} AND k.index_name = 'PRIMARY'
              AND k.column_name = {column} AND k.sub_part IS NULL AND c.data_type = 'varchar'
              AND c.character_maximum_length >= {maximumIdLength}
              AND c.collation_name IN ('utf8mb4_0900_ai_ci', 'utf8mb4_general_ci', 'utf8mb4_unicode_ci', 'utf8mb4_bin')
              AND NOT EXISTS (SELECT 1 FROM information_schema.statistics x WHERE x.table_schema = k.table_schema
                              AND x.table_name = k.table_name AND x.non_unique = 0
                              AND (x.index_name <> 'PRIMARY' OR x.seq_in_index > 1))
            """).SingleAsync(cancellationToken) == 1;
    }
}
