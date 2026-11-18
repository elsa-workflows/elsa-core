using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Elsa.Persistence.EFCore.Extensions;

internal static class PostgreSqlStoreKeyVerifier
{
    internal static async Task<bool> VerifyAsync(DbContext context, StoreObjectIdentifier table, string column, int maximumIdLength, CancellationToken cancellationToken)
    {
        var relation = context.GetService<ISqlGenerationHelper>().DelimitIdentifier(table.Name, table.Schema);
        return await context.Database.SqlQuery<bool>($"""
            SELECT EXISTS (
                SELECT 1 FROM pg_catalog.pg_index i
                JOIN pg_catalog.pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = i.indkey[0]
                JOIN pg_catalog.pg_opclass op ON op.oid = i.indclass[0]
                JOIN pg_catalog.pg_collation c ON c.oid = i.indcollation[0]
                WHERE i.indrelid = pg_catalog.to_regclass({relation}) AND i.indisprimary AND i.indnkeyatts = 1
                  AND a.attname = {column} AND a.atttypid IN ('pg_catalog.text'::regtype, 'pg_catalog.varchar'::regtype)
                  AND a.atttypmod = -1 AND op.opcnamespace = 'pg_catalog'::regnamespace AND op.opcname = 'text_ops'
                  AND a.attcollation = i.indcollation[0] AND c.collisdeterministic
            ) AS "Value"
            """).SingleAsync(cancellationToken);
    }
}
