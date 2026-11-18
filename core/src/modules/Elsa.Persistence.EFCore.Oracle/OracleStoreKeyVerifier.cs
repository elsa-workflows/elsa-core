using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.Persistence.EFCore.Extensions;

internal static class OracleStoreKeyVerifier
{
    internal static async Task<bool> VerifyAsync(DbContext context, StoreObjectIdentifier table, string column, int maximumIdLength, CancellationToken cancellationToken)
    {
        return await context.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS "Value"
            FROM all_constraints p
            JOIN all_cons_columns k ON k.owner = p.owner AND k.constraint_name = p.constraint_name
            JOIN all_tab_columns c ON c.owner = k.owner AND c.table_name = k.table_name AND c.column_name = k.column_name
            WHERE p.owner = COALESCE(CAST({table.Schema} AS VARCHAR2(128)), SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'))
              AND p.table_name = {table.Name} AND p.constraint_type = 'P' AND p.status = 'ENABLED'
              AND k.position = 1 AND k.column_name = {column} AND c.data_type = 'NVARCHAR2'
              AND c.char_length >= {maximumIdLength}
              AND (c.collation = 'BINARY' OR (c.collation = 'USING_NLS_COMP' AND EXISTS
                   (SELECT 1 FROM nls_session_parameters WHERE parameter = 'NLS_COMP' AND value = 'BINARY')))
              AND NOT EXISTS (SELECT 1 FROM all_cons_columns x WHERE x.owner = p.owner
                              AND x.constraint_name = p.constraint_name AND x.position > 1)
            """).SingleAsync(cancellationToken) == 1;
    }
}
