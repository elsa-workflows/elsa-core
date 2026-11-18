using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.Persistence.EFCore.Extensions;

internal static class SqliteStoreKeyVerifier
{
    internal static async Task<bool> VerifyAsync(DbContext context, StoreObjectIdentifier table, string column, int maximumIdLength, CancellationToken cancellationToken)
    {
        return await context.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS "Value"
            FROM pragma_index_list({table.Name}, 'main') i
            JOIN pragma_index_xinfo(i.name, 'main') k ON k.key = 1 AND k.name = {column}
            JOIN pragma_table_xinfo({table.Name}, 'main') c ON c.name = k.name
            WHERE i.origin = 'pk' AND k.coll = 'BINARY' AND upper(c.type) = 'TEXT'
              AND (SELECT COUNT(*) FROM pragma_index_xinfo(i.name, 'main') WHERE key = 1) = 1
              AND EXISTS (SELECT 1 FROM main.sqlite_master WHERE type = 'table' AND name = {table.Name}
                          AND instr(upper(sql), 'COLLATE') = 0)
              AND NOT EXISTS (SELECT 1 FROM sqlite_temp_master WHERE name = {table.Name})
            """).SingleAsync(cancellationToken) == 1;
    }
}
