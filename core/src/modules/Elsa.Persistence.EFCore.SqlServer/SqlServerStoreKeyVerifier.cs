using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Elsa.Persistence.EFCore.Extensions;

internal static class SqlServerStoreKeyVerifier
{
    internal static async Task<bool> VerifyAsync(DbContext context, StoreObjectIdentifier table, string column, int maximumIdLength, CancellationToken cancellationToken)
    {
        var relation = context.GetService<ISqlGenerationHelper>().DelimitIdentifier(table.Name, table.Schema);
        return await context.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS [Value]
            FROM sys.indexes i
            JOIN sys.index_columns k ON k.object_id = i.object_id AND k.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = k.object_id AND c.column_id = k.column_id
            WHERE i.object_id = OBJECT_ID({relation}, 'U') AND i.is_primary_key = 1 AND k.key_ordinal = 1
              AND c.name = {column} AND c.system_type_id = TYPE_ID('nvarchar')
              AND c.max_length >= 2 * {maximumIdLength} AND c.is_computed = 0
              AND c.collation_name IN ('SQL_Latin1_General_CP1_CI_AS', 'Latin1_General_100_CI_AS',
                                       'Latin1_General_100_BIN2', 'Latin1_General_BIN2')
              AND NOT EXISTS (SELECT 1 FROM sys.index_columns x WHERE x.object_id = i.object_id
                              AND x.index_id = i.index_id AND x.key_ordinal > 1)
            """).SingleAsync(cancellationToken) == 1;
    }
}
