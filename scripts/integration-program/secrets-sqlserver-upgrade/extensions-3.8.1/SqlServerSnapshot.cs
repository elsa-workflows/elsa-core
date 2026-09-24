using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

internal static class SqlServerSnapshot
{
    public static async Task<int> SeedAsync(string connectionString)
    {
        const string sql = """
            INSERT INTO [Elsa].[Secrets]
              ([Id], [SecretId], [Name], [Scope], [EncryptedValue], [Description], [Version], [IsLatest],
               [Status], [ExpiresIn], [ExpiresAt], [LastAccessedAt], [TenantId], [CreatedAt], [UpdatedAt], [Owner])
            VALUES
              (N'synthetic-row-version-1', N'synthetic-secret-for-upgrade-fixture', N'synthetic-credential', N'fixture-only',
               N'SYNTHETIC-CIPHERTEXT-SENTINEL-V1', N'synthetic fixture row; not a credential', 1, 0, 0,
               CAST('00:15:00' AS time), CAST('2030-01-02T03:04:05+00:00' AS datetimeoffset),
               CAST('2026-09-23T10:00:00+00:00' AS datetimeoffset), N'synthetic-tenant',
               CAST('2026-09-23T09:00:00+00:00' AS datetimeoffset),
               CAST('2026-09-23T09:00:00+00:00' AS datetimeoffset), N'synthetic-owner'),
              (N'synthetic-row-version-2', N'synthetic-secret-for-upgrade-fixture', N'synthetic-credential', N'fixture-only',
               N'SYNTHETIC-CIPHERTEXT-SENTINEL-V2', N'synthetic fixture row; not a credential', 2, 1, 0,
               CAST('00:15:00' AS time), CAST('2030-01-02T03:04:05+00:00' AS datetimeoffset),
               CAST('2026-09-23T10:00:00+00:00' AS datetimeoffset), N'synthetic-tenant',
               CAST('2026-09-23T09:00:00+00:00' AS datetimeoffset),
               CAST('2026-09-23T10:00:00+00:00' AS datetimeoffset), N'synthetic-owner');
            """;
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return await command.ExecuteNonQueryAsync();
    }

    public static async Task<object> CaptureAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        var columns = await QueryJsonAsync(connection, """
            SELECT t.[name] AS [table], c.[name] AS [name], ty.[name] AS [dataType],
                   c.[max_length] AS [maxLength], c.[is_nullable] AS [nullable]
            FROM sys.columns c
            JOIN sys.tables t ON t.object_id = c.object_id
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            WHERE s.[name] = N'Elsa'
            ORDER BY t.[name], c.column_id
            FOR JSON PATH
            """);
        var indexes = await QueryJsonAsync(connection, """
            SELECT t.[name] AS [table], i.[name] AS [name], i.[is_unique] AS [unique],
                   i.[is_primary_key] AS [primaryKey]
            FROM sys.indexes i
            JOIN sys.tables t ON t.object_id = i.object_id
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE s.[name] = N'Elsa' AND i.[name] IS NOT NULL
            ORDER BY t.[name], i.[name]
            FOR JSON PATH
            """);
        var migrationHistory = await QueryJsonAsync(connection, """
            SELECT [MigrationId], [ProductVersion]
            FROM [Elsa].[__EFMigrationsHistory]
            ORDER BY [MigrationId]
            FOR JSON PATH
            """);
        var syntheticRows = await QueryJsonAsync(connection, """
            SELECT [Id], [SecretId], [Name], [Scope], [Description], [Version], [IsLatest], [Status],
                   [TenantId], [Owner], CONVERT(varchar(8), [ExpiresIn], 108) AS [ExpiresIn],
                   CONVERT(varchar(19), SWITCHOFFSET([ExpiresAt], '+00:00'), 126) + 'Z' AS [ExpiresAt],
                   CONVERT(varchar(19), SWITCHOFFSET([LastAccessedAt], '+00:00'), 126) + 'Z' AS [LastAccessedAt],
                   CONVERT(varchar(19), SWITCHOFFSET([CreatedAt], '+00:00'), 126) + 'Z' AS [CreatedAt],
                   CONVERT(varchar(19), SWITCHOFFSET([UpdatedAt], '+00:00'), 126) + 'Z' AS [UpdatedAt],
                   LOWER(CONVERT(varchar(128), HASHBYTES('SHA2_512', CONVERT(varchar(max), [EncryptedValue])), 2))
                       AS [encryptedValueSha512]
            FROM [Elsa].[Secrets]
            ORDER BY [Version]
            FOR JSON PATH, INCLUDE_NULL_VALUES
            """);
        return new { columns, indexes, migrationHistory, syntheticRows };
    }

    private static async Task<JsonNode> QueryJsonAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new StringBuilder();
        while (await reader.ReadAsync())
        {
            result.Append(reader.GetString(0));
        }
        return JsonNode.Parse(result.ToString()) ?? throw new InvalidOperationException("SQL Server returned no snapshot JSON");
    }
}
